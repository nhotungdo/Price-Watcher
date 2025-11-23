using System.Text.Json;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services.Scrapers;

public class LazadaScraperStub : IProductScraper
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _rate = new(5);
    private readonly IMetricsService _metrics;
    private readonly ILogger<LazadaScraperStub> _logger;
    private static readonly SemaphoreSlim _robotsLock = new(1,1);
    private static volatile bool _robotsLoaded;
    private static readonly HashSet<string> _disallow = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string,(DateTime ts, List<ProductCandidateDto> items)> _searchCache = new();
    private static readonly ConcurrentDictionary<string,(DateTime ts, ProductCandidateDto? item)> _detailCache = new();
    private const int CacheTtlSeconds = 120;

    public LazadaScraperStub(IHttpClientFactory httpFactory, IMetricsService metrics, ILogger<LazadaScraperStub> logger)
    {
        _http = httpFactory.CreateClient("lazada");
        _metrics = metrics;
        _logger = logger;
    }

    public string Platform => "lazada";

    public async Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _rate.WaitAsync(cancellationToken);
        try
        {
            await EnsureRobotsAsync(cancellationToken);
            var keyword = string.IsNullOrWhiteSpace(query.TitleHint) ? query.ProductId : query.TitleHint!;
            var notUseful = Regex.IsMatch(keyword ?? string.Empty, @"^pdp\s+i\d+\s+s\d+$", RegexOptions.IgnoreCase) || Regex.IsMatch(keyword ?? string.Empty, @"^\d+$");
            if (notUseful && !string.IsNullOrWhiteSpace(query.CanonicalUrl))
            {
                try
                {
                    var pdHtml = await _http.GetStringAsync(query.CanonicalUrl, cancellationToken);
                    var mTitle = Regex.Match(pdHtml, @"<title>(.*?)</title>", RegexOptions.Singleline);
                    if (mTitle.Success)
                    {
                        var t = System.Net.WebUtility.HtmlDecode(mTitle.Groups[1].Value);
                        t = Regex.Replace(t, @"\s+\|\s+Lazada.*$", string.Empty, RegexOptions.IgnoreCase).Trim();
                        if (!string.IsNullOrWhiteSpace(t)) keyword = t;
                    }
                    else
                    {
                        var scripts = Regex.Matches(pdHtml, "<script type=\"application/ld\\+json\">([\\s\\S]*?)</script>", RegexOptions.Singleline);
                        foreach (Match mx in scripts)
                        {
                            try
                            {
                                using var jd = JsonDocument.Parse(mx.Groups[1].Value);
                                var root = jd.RootElement;
                                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("name", out var nm))
                                {
                                    var t = nm.GetString() ?? string.Empty;
                                    if (!string.IsNullOrWhiteSpace(t)) { keyword = t; break; }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
            var safeKeyword = (keyword ?? string.Empty);
            var cacheKey = safeKeyword.Trim().ToLowerInvariant();
            if (_searchCache.TryGetValue(cacheKey, out var cached) && (DateTime.UtcNow - cached.ts).TotalSeconds < CacheTtlSeconds)
            {
                return cached.items;
            }
            var searchUrl = $"/search?q={Uri.EscapeDataString(safeKeyword)}";
            if (!IsAllowed("/search")) return Array.Empty<ProductCandidateDto>();
            var req = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            req.Headers.Referrer = new Uri("https://www.lazada.vn/");
            req.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            req.Headers.AcceptLanguage.ParseAdd("vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            var res = await _http.SendAsync(req, cancellationToken);
            var html = await res.Content.ReadAsStringAsync(cancellationToken);
            var m = Regex.Match(html, @"listItems\s*:\s*\[(.*?)\]\s*,\s*totalResults", RegexOptions.Singleline);
            var list = new List<ProductCandidateDto>();
            if (m.Success)
            {
                var jsonArray = "[" + m.Groups[1].Value + "]";
                using var doc = JsonDocument.Parse(jsonArray);
                foreach (var it in doc.RootElement.EnumerateArray())
                {
                    TryAddLazadaItem(it, list, query);
                }
            }
            else
            {
                var mPageData = Regex.Match(html, @"window\.pageData\s*=\s*(\{[\s\S]*?\});", RegexOptions.Singleline);
                if (mPageData.Success)
                {
                    using var pd = JsonDocument.Parse(mPageData.Groups[1].Value);
                    ExtractLazadaProductsFromJsonElement(pd.RootElement, list, query);
                }
                var mLzData = Regex.Match(html, @"window\.__LZ_DATA__\s*=\s*(\{[\s\S]*?\});", RegexOptions.Singleline);
                if (list.Count == 0 && mLzData.Success)
                {
                    using var ld = JsonDocument.Parse(mLzData.Groups[1].Value);
                    ExtractLazadaProductsFromJsonElement(ld.RootElement, list, query);
                }
                var mState = Regex.Match(html, @"window\.__LAZADA_STATE__\s*=\s*(\{[\s\S]*?\});", RegexOptions.Singleline);
                if (list.Count == 0 && mState.Success)
                {
                    using var st = JsonDocument.Parse(mState.Groups[1].Value);
                    ExtractLazadaProductsFromJsonElement(st.RootElement, list, query);
                }
                if (list.Count == 0)
                {
                    var catalogUrl = $"/catalog/?q={Uri.EscapeDataString(safeKeyword)}&_keyori=ss&from=input";
                    if (!IsAllowed("/catalog/")) return Array.Empty<ProductCandidateDto>();
                    var creq = new HttpRequestMessage(HttpMethod.Get, catalogUrl);
                    creq.Headers.Referrer = new Uri("https://www.lazada.vn/");
                    creq.Headers.Accept.ParseAdd("text/html,application/json");
                    var cres = await _http.SendAsync(creq, cancellationToken);
                    var chtml = await cres.Content.ReadAsStringAsync(cancellationToken);
                    var cm = Regex.Match(chtml, @"listItems\s*:\s*\[(.*?)\]\s*,\s*totalResults", RegexOptions.Singleline);
                    if (cm.Success)
                    {
                        var jsonArray2 = "[" + cm.Groups[1].Value + "]";
                        using var doc2 = JsonDocument.Parse(jsonArray2);
                        foreach (var it in doc2.RootElement.EnumerateArray()) TryAddLazadaItem(it, list, query);
                    }
                }
            }
            _logger.LogInformation("Lazada parsed {Count} items for {Keyword}", list.Count, keyword);
            _searchCache[cacheKey] = (DateTime.UtcNow, list);
            return list;
        }
        finally
        {
            _rate.Release();
            sw.Stop();
            _metrics.RecordScraperCall(Platform, success: true, elapsedMs: sw.ElapsedMilliseconds);
        }
    }

    public async Task<ProductCandidateDto?> GetByUrlAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(query.Platform, Platform, StringComparison.OrdinalIgnoreCase)) return null;
        if (string.IsNullOrWhiteSpace(query.CanonicalUrl)) return null;
        var html = await _http.GetStringAsync(query.CanonicalUrl, cancellationToken);
        var m = Regex.Match(html, @"window\.pageData\s*=\s*(\{[\s\S]*?\});", RegexOptions.Singleline);
        if (m.Success)
        {
            using var doc = JsonDocument.Parse(m.Groups[1].Value);
            var root = doc.RootElement;
            if (root.TryGetProperty("mods", out var mods))
            {
                string title = string.Empty, sellerName = "Lazada", productUrl = query.CanonicalUrl!, thumb = string.Empty; decimal price = 0m; double rating = 0; int reviews = 0; int sold = 0; decimal? originalPrice = null; double? discountPct = null;
                if (mods.TryGetProperty("productInfo", out var pi) && pi.ValueKind == JsonValueKind.Object)
                {
                    if (pi.TryGetProperty("title", out var t)) title = t.GetString() ?? string.Empty;
                }
                if (mods.TryGetProperty("item", out var it) && it.ValueKind == JsonValueKind.Object)
                {
                    if (it.TryGetProperty("name", out var n)) title = string.IsNullOrEmpty(title) ? (n.GetString() ?? string.Empty) : title;
                    if (it.TryGetProperty("price", out var p)) price = p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : ParsePriceString(p.GetString());
                    if (it.TryGetProperty("ratingScore", out var rs)) rating = rs.GetDouble();
                    if (it.TryGetProperty("review", out var rv)) reviews = rv.GetInt32();
                    if (it.TryGetProperty("image", out var img)) thumb = img.GetString() ?? string.Empty;
                    if (it.TryGetProperty("sellerName", out var sn)) sellerName = sn.GetString() ?? sellerName;
                    if (it.TryGetProperty("originalPrice", out var op) && (op.ValueKind == JsonValueKind.String || op.ValueKind == JsonValueKind.Number))
                    {
                        originalPrice = op.ValueKind == JsonValueKind.Number ? op.GetDecimal() : ParsePriceStringNullable(op.GetString());
                        if (originalPrice.HasValue && originalPrice > 0 && price > 0) discountPct = (double)((originalPrice.Value - price) / originalPrice.Value);
                    }
                }
                var dto = new ProductCandidateDto
                {
                    Platform = Platform,
                    Title = title,
                    Price = price,
                    ShippingCost = 0,
                    ShopName = sellerName,
                    ShopRating = rating,
                    ShopSales = reviews,
                    ProductUrl = productUrl,
                    ThumbnailUrl = thumb,
                    SoldCount = sold,
                    OriginalPrice = originalPrice,
                    DiscountPercent = discountPct,
                    SellerType = sellerName.Contains("LazMall", StringComparison.OrdinalIgnoreCase) ? "Chính hãng (LazMall)" : "Đại lý"
                };
                _detailCache[query.CanonicalUrl.Trim().ToLowerInvariant()] = (DateTime.UtcNow, dto);
                dto.IsOutOfStock = Regex.IsMatch(html, "hết hàng|out of stock", RegexOptions.IgnoreCase);
                var hasVoucher = Regex.IsMatch(html, "voucher", RegexOptions.IgnoreCase);
                var hasFree = Regex.IsMatch(html, "miễn phí vận chuyển|free shipping", RegexOptions.IgnoreCase);
                dto.IsFreeShip = hasFree;
                dto.PromotionSummary = (hasVoucher || hasFree) ? string.Join(", ", new[]{ hasVoucher ? "Voucher" : null, hasFree ? "Freeship" : null }.Where(x => x != null)) : null;
                if (dto.Price == 0)
                {
                    var mState = Regex.Match(html, @"window\.__LAZADA_STATE__\s*=\s*(\{[\s\S]*?\});", RegexOptions.Singleline);
                    if (mState.Success)
                    {
                        try
                        {
                            using var st = JsonDocument.Parse(mState.Groups[1].Value);
                            var p2 = ExtractPriceFromState(st.RootElement);
                            if (p2 > 0) dto.Price = p2;
                        }
                        catch { }
                    }
                    var mLz = Regex.Match(html, @"window\.__LZ_DATA__\s*=\s*(\{[\s\S]*?\});", RegexOptions.Singleline);
                    if (dto.Price == 0 && mLz.Success)
                    {
                        try
                        {
                            using var ld = JsonDocument.Parse(mLz.Groups[1].Value);
                            var p3 = ExtractPriceFromState(ld.RootElement);
                            if (p3 > 0) dto.Price = p3;
                        }
                        catch { }
                    }
                }
                return dto;
            }
        }

        var scripts = Regex.Matches(html, "<script type=\"application/ld\\+json\">([\\s\\S]*?)</script>", RegexOptions.Singleline);
        foreach (Match mx in scripts)
        {
            try
            {
                using var doc = JsonDocument.Parse(mx.Groups[1].Value);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) continue;
                var ok = !root.TryGetProperty("@type", out var tp) || (tp.ValueKind == JsonValueKind.String && string.Equals(tp.GetString(), "Product", StringComparison.OrdinalIgnoreCase));
                if (!ok) continue;
                var title = root.TryGetProperty("name", out var nm) ? nm.GetString() ?? string.Empty : string.Empty;
                decimal price = 0m;
                if (root.TryGetProperty("offers", out var offers))
                {
                    if (offers.ValueKind == JsonValueKind.Object)
                    {
                        if (offers.TryGetProperty("price", out var p) && (p.ValueKind == JsonValueKind.Number || p.ValueKind == JsonValueKind.String))
                        {
                            price = p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : (decimal.TryParse(p.GetString(), out var dp) ? dp : 0m);
                        }
                        else if (offers.TryGetProperty("lowPrice", out var lp) && lp.ValueKind == JsonValueKind.Number)
                        {
                            price = lp.GetDecimal();
                        }
                    }
                    else if (offers.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var off in offers.EnumerateArray())
                        {
                            if (off.ValueKind != JsonValueKind.Object) continue;
                            if (off.TryGetProperty("price", out var p2) && (p2.ValueKind == JsonValueKind.Number || p2.ValueKind == JsonValueKind.String))
                            {
                                var v = p2.ValueKind == JsonValueKind.Number ? p2.GetDecimal() : (decimal.TryParse(p2.GetString(), out var dp2) ? dp2 : 0m);
                                if (price == 0m || (v > 0m && v < price)) price = v;
                            }
                        }
                    }
                }
                var rating = 0d;
                if (root.TryGetProperty("aggregateRating", out var ar) && ar.ValueKind == JsonValueKind.Object && ar.TryGetProperty("ratingValue", out var rv))
                {
                    if (rv.ValueKind == JsonValueKind.Number) rating = rv.GetDouble();
                    else if (rv.ValueKind == JsonValueKind.String && double.TryParse(rv.GetString(), out var dr)) rating = dr;
                }
                var image = root.TryGetProperty("image", out var img) && img.ValueKind == JsonValueKind.String ? img.GetString() ?? string.Empty : string.Empty;
                var sellerName = "Lazada";
                if (root.TryGetProperty("brand", out var br) && br.ValueKind == JsonValueKind.Object && br.TryGetProperty("name", out var bn) && bn.ValueKind == JsonValueKind.String)
                {
                    sellerName = bn.GetString() ?? sellerName;
                }
                var thumb = image;
                return new ProductCandidateDto
                {
                    Platform = Platform,
                    Title = title,
                    Price = price,
                    ShippingCost = 0,
                    ShopName = sellerName,
                    ShopRating = rating,
                    ShopSales = 0,
                    ProductUrl = query.CanonicalUrl,
                    ThumbnailUrl = thumb
                };
            }
            catch
            {
            }
        }
        return null;
    }

    private static void ExtractLazadaProductsFromJsonElement(JsonElement root, List<ProductCandidateDto> list, ProductQuery query)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                ExtractLazadaProductsFromJsonElement(item, list, query);
            }
            return;
        }
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("listItems", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in items.EnumerateArray()) TryAddLazadaItem(it, list, query);
            }
            foreach (var prop in root.EnumerateObject())
            {
                ExtractLazadaProductsFromJsonElement(prop.Value, list, query);
            }
        }
    }

    private static void TryAddLazadaItem(JsonElement it, List<ProductCandidateDto> list, ProductQuery query)
    {
        if (it.ValueKind != JsonValueKind.Object) return;
        var hasName = it.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String;
        var hasPrice = it.TryGetProperty("price", out var p) && (p.ValueKind == JsonValueKind.String || p.ValueKind == JsonValueKind.Number);
        if (!hasName || !hasPrice) return;
        var title = n.GetString() ?? string.Empty;
        var price = GetPrice(it);
        var rating = it.TryGetProperty("ratingScore", out var rs) && rs.ValueKind == JsonValueKind.Number ? rs.GetDouble() : (rs.ValueKind == JsonValueKind.String && double.TryParse(rs.GetString(), out var drs) ? drs : 0);
        var reviews = it.TryGetProperty("review", out var rv) && rv.ValueKind == JsonValueKind.Number ? rv.GetInt32() : 0;
        var thumb = it.TryGetProperty("image", out var img) && img.ValueKind == JsonValueKind.String ? img.GetString() ?? string.Empty : string.Empty;
        var sellerName = it.TryGetProperty("sellerName", out var sn) && sn.ValueKind == JsonValueKind.String ? sn.GetString() ?? "Lazada" : "Lazada";
        var productUrl = it.TryGetProperty("productUrl", out var pu) && pu.ValueKind == JsonValueKind.String ? pu.GetString() ?? string.Empty : (it.TryGetProperty("itemUrl", out var iu) && iu.ValueKind == JsonValueKind.String ? iu.GetString() ?? string.Empty : string.Empty);
        var sold = it.TryGetProperty("itemSoldCnt", out var sc) && sc.ValueKind == JsonValueKind.Number ? sc.GetInt32() : (it.TryGetProperty("soldCount", out var sc2) && sc2.ValueKind == JsonValueKind.Number ? sc2.GetInt32() : 0);
        var originalPrice = it.TryGetProperty("originalPrice", out var op) ? (op.ValueKind == JsonValueKind.Number ? op.GetDecimal() : ParsePriceStringNullable(op.GetString())) : (decimal?)null;
        if (price == 0m && originalPrice.HasValue) price = originalPrice.Value;
        double? discountPct = null;
        if (originalPrice.HasValue && originalPrice > 0 && price > 0)
            discountPct = (double)((originalPrice.Value - price) / originalPrice.Value);
        var sellerType = it.TryGetProperty("itemBadge", out var badge) && badge.ValueKind == JsonValueKind.Array && badge.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && x.GetString()?.Contains("LazMall", StringComparison.OrdinalIgnoreCase) == true) ? "Chính hãng (LazMall)" : "Đại lý";
        if (!string.IsNullOrWhiteSpace(productUrl) && productUrl.StartsWith("/")) productUrl = "https://www.lazada.vn" + productUrl;
        if (!string.IsNullOrWhiteSpace(thumb) && thumb.StartsWith("//")) thumb = "https:" + thumb;
        list.Add(new ProductCandidateDto
        {
            Platform = "lazada",
            Title = title,
            Price = price,
            ShippingCost = 0,
            ShopName = sellerName,
            ShopRating = rating,
            ShopSales = reviews,
            ProductUrl = string.IsNullOrWhiteSpace(productUrl) ? query.CanonicalUrl : productUrl,
            ThumbnailUrl = thumb,
            SoldCount = sold,
            OriginalPrice = originalPrice,
            DiscountPercent = discountPct,
            SellerType = sellerType
        });
    }

    private static decimal GetPrice(JsonElement it)
    {
        decimal price = 0m;
        if (it.TryGetProperty("price", out var p)) price = p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : ParsePriceString(p.GetString());
        if (price == 0m && it.TryGetProperty("priceShow", out var ps)) price = ps.ValueKind == JsonValueKind.Number ? ps.GetDecimal() : ParsePriceString(ps.GetString());
        if (price == 0m && it.TryGetProperty("salePrice", out var sp)) price = sp.ValueKind == JsonValueKind.Number ? sp.GetDecimal() : ParsePriceString(sp.GetString());
        if (price == 0m && it.TryGetProperty("discountPrice", out var dp)) price = dp.ValueKind == JsonValueKind.Number ? dp.GetDecimal() : ParsePriceString(dp.GetString());
        if (price == 0m && it.TryGetProperty("priceMin", out var pmin)) price = pmin.ValueKind == JsonValueKind.Number ? pmin.GetDecimal() : ParsePriceString(pmin.GetString());
        if (price == 0m && it.TryGetProperty("priceMax", out var pmax)) price = pmax.ValueKind == JsonValueKind.Number ? pmax.GetDecimal() : ParsePriceString(pmax.GetString());
        if (price == 0m && it.TryGetProperty("sku", out var sku) && sku.ValueKind == JsonValueKind.Object && sku.TryGetProperty("price", out var skp)) price = skp.ValueKind == JsonValueKind.Number ? skp.GetDecimal() : ParsePriceString(skp.GetString());
        if (price == 0m && it.TryGetProperty("skuInfos", out var si) && si.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in si.EnumerateArray())
            {
                if (s.ValueKind != JsonValueKind.Object) continue;
                if (s.TryGetProperty("price", out var sip))
                {
                    var v = sip.ValueKind == JsonValueKind.Number ? sip.GetDecimal() : ParsePriceString(sip.GetString());
                    if (v > 0m && (price == 0m || v < price)) price = v;
                }
            }
        }
        return price;
    }

    private static decimal ParsePriceString(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        var digits = System.Text.RegularExpressions.Regex.Replace(s, @"[^0-9]", "");
        if (string.IsNullOrEmpty(digits)) return 0m;
        return long.TryParse(digits, out var lv) ? (decimal)lv : 0m;
    }

    private static decimal? ParsePriceStringNullable(string? s)
    {
        var v = ParsePriceString(s);
        return v == 0m ? (decimal?)null : v;
    }
    private static decimal ExtractPriceFromState(JsonElement root)
    {
        try
        {
            if (root.ValueKind != JsonValueKind.Object) return 0m;
            if (root.TryGetProperty("root", out var r) && r.ValueKind == JsonValueKind.Object)
            {
                if (r.TryGetProperty("pdpData", out var pd) && pd.ValueKind == JsonValueKind.Object)
                {
                    if (pd.TryGetProperty("skuInfos", out var si) && si.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var sku in si.EnumerateArray())
                        {
                            if (sku.ValueKind != JsonValueKind.Object) continue;
                            if (sku.TryGetProperty("price", out var pr))
                            {
                                if (pr.ValueKind == JsonValueKind.Number) return pr.GetDecimal();
                                if (pr.ValueKind == JsonValueKind.String)
                                {
                                    var s = pr.GetString();
                                    var d = ParsePriceString(s);
                                    if (d > 0) return d;
                                }
                                if (pr.ValueKind == JsonValueKind.Object)
                                {
                                    if (pr.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number) return v.GetDecimal();
                                    if (pr.TryGetProperty("minPrice", out var mp))
                                    {
                                        if (mp.ValueKind == JsonValueKind.Number) return mp.GetDecimal();
                                        if (mp.ValueKind == JsonValueKind.String)
                                        {
                                            var s = mp.GetString();
                                            var d = ParsePriceString(s);
                                            if (d > 0) return d;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (var prop in root.EnumerateObject())
            {
                var v = prop.Value;
                var p = ExtractPriceFromState(v);
                if (p > 0) return p;
            }
        }
        catch { }
        return 0m;
    }

    private async Task EnsureRobotsAsync(CancellationToken ct)
    {
        if (_robotsLoaded) return;
        await _robotsLock.WaitAsync(ct);
        try
        {
            if (_robotsLoaded) return;
            var res = await _http.GetAsync("/robots.txt", ct);
            var txt = await res.Content.ReadAsStringAsync(ct);
            var lines = txt.Split('\n');
            var uaAny = false;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
                {
                    uaAny = line.Contains("*");
                    continue;
                }
                if (!uaAny) continue;
                if (line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
                {
                    var path = line.Substring("Disallow:".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(path)) _disallow.Add(path);
                }
            }
            _robotsLoaded = true;
        }
        finally
        {
            _robotsLock.Release();
        }
    }

    private static bool IsAllowed(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        foreach (var d in _disallow)
        {
            if (string.IsNullOrWhiteSpace(d)) continue;
            if (d == "/") return false;
            if (path.StartsWith(d, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }
}

