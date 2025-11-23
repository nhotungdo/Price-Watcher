using System.Text.Json;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services.Scrapers;

public class ShopeeScraperStub : IProductScraper
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _rate = new(5);
    private readonly IMetricsService _metrics;
    private readonly ILogger<ShopeeScraperStub> _logger;

    public ShopeeScraperStub(IHttpClientFactory httpFactory, IMetricsService metrics, ILogger<ShopeeScraperStub> logger)
    {
        _http = httpFactory.CreateClient("shopee");
        _metrics = metrics;
        _logger = logger;
    }

    public string Platform => "shopee";

    public async Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _rate.WaitAsync(cancellationToken);
        try
        {
            var keyword = string.IsNullOrWhiteSpace(query.TitleHint) ? query.ProductId : query.TitleHint!;
            // làm sạch keyword nếu chứa chuỗi id Shopee
            keyword = System.Text.RegularExpressions.Regex.Replace(keyword, @"\bi\.\d+\.\d+\b", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            // preflight để lấy cookie/session hợp lệ
            var preflight = new HttpRequestMessage(HttpMethod.Get, $"https://shopee.vn/search?keyword={Uri.EscapeDataString(keyword)}");
            preflight.Headers.Referrer = new Uri("https://shopee.vn/");
            await _http.SendAsync(preflight, cancellationToken);
            var url = $"https://shopee.vn/api/v4/search/search_items?by=relevancy&order=desc&keyword={Uri.EscapeDataString(keyword)}&limit=20&newest=0";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Referrer = new Uri($"https://shopee.vn/search?keyword={Uri.EscapeDataString(keyword)}");
            req.Headers.Add("x-api-source", "pc");
            req.Headers.Add("x-shopee-language", "vi");
            req.Headers.Add("x-requested-with", "XMLHttpRequest");
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            var res = await _http.SendAsync(req, cancellationToken);
            var list = new List<ProductCandidateDto>();
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
                if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in items.EnumerateArray())
                    {
                        var itemid = it.TryGetProperty("item_basic", out var b) && b.TryGetProperty("itemid", out var iid) ? iid.GetInt64() : (it.TryGetProperty("itemid", out var iid2) ? iid2.GetInt64() : 0);
                        var shopid = it.TryGetProperty("item_basic", out var b2) && b2.TryGetProperty("shopid", out var sid) ? sid.GetInt64() : (it.TryGetProperty("shopid", out var sid2) ? sid2.GetInt64() : 0);
                        var title = it.TryGetProperty("item_basic", out var b3) && b3.TryGetProperty("name", out var nm) ? nm.GetString() ?? string.Empty : (it.TryGetProperty("name", out var nm2) ? nm2.GetString() ?? string.Empty : string.Empty);
                        var priceRaw = it.TryGetProperty("item_basic", out var b4) && b4.TryGetProperty("price", out var pr) ? pr.GetInt64() : (it.TryGetProperty("price", out var pr2) ? pr2.GetInt64() : 0);
                        var price = NormalizeShopeePrice(priceRaw);
                        var image = it.TryGetProperty("item_basic", out var b5) && b5.TryGetProperty("image", out var im) ? im.GetString() ?? string.Empty : (it.TryGetProperty("image", out var im2) ? im2.GetString() ?? string.Empty : string.Empty);
                        var rating = it.TryGetProperty("item_basic", out var b6) && b6.TryGetProperty("shopee_verified", out var sv) ? (sv.GetBoolean() ? 4.5 : 4.0) : 0;
                        int sold = 0;
                        if (it.TryGetProperty("item_basic", out var b7))
                        {
                            sold = b7.TryGetProperty("historical_sold", out var hs) ? hs.GetInt32() : (b7.TryGetProperty("sold", out var sd) ? sd.GetInt32() : 0);
                        }
                        var priceBefore = it.TryGetProperty("item_basic", out var b8) && b8.TryGetProperty("price_before_discount", out var pbd) ? pbd.GetInt64() : 0;
                        var isOfficial = it.TryGetProperty("item_basic", out var b9) && b9.TryGetProperty("is_official_shop", out var ios) ? ios.GetBoolean() : false;
                        decimal? originalPrice = null;
                        double? discountPct = null;
                        if (priceBefore > 0)
                        {
                            var before = NormalizeShopeePrice(priceBefore);
                            originalPrice = before;
                            if (before > 0 && price > 0)
                            {
                                discountPct = (double)((before - price) / before);
                            }
                        }
                        var productUrl = (itemid > 0 && shopid > 0) ? $"https://shopee.vn/product/{shopid}/{itemid}" : query.CanonicalUrl;
                        var thumb = string.IsNullOrWhiteSpace(image) ? string.Empty : $"https://cf.shopee.vn/file/{image}";
                        list.Add(new ProductCandidateDto
                        {
                            Platform = Platform,
                            Title = title,
                            Price = price,
                            ShippingCost = 0,
                            ShopName = shopid > 0 ? $"Shop {shopid}" : "Shopee",
                            ShopRating = rating,
                            ShopSales = 0,
                            SoldCount = sold,
                            OriginalPrice = originalPrice,
                            DiscountPercent = discountPct,
                            SellerType = isOfficial ? "Chính hãng" : "Đại lý",
                            ProductUrl = productUrl,
                            ThumbnailUrl = thumb
                        });
                    }
                }
            }
            else
            {
                var payloadSnippet = await res.Content.ReadAsStringAsync(cancellationToken);
                if (payloadSnippet.Length > 256) payloadSnippet = payloadSnippet[..256];
                _logger.LogWarning("Shopee search_items returned {Status} for {Keyword}. Snippet={Snippet}", res.StatusCode, keyword, payloadSnippet);
            }
            _logger.LogInformation("Shopee parsed {Count} items for {Keyword}", list.Count, keyword);
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
        if (string.IsNullOrWhiteSpace(query.ProductId) || !query.ProductId.StartsWith("i.")) return null;
        var parts = query.ProductId.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return null;
        if (!long.TryParse(parts[1], out var shopId)) return null;
        if (!long.TryParse(parts[2], out var itemId)) return null;

        var preflightUrl = query.CanonicalUrl ?? $"https://shopee.vn/product/{shopId}/{itemId}";
        
        // 1. Try API first
        try
        {
            var apiUrl = $"https://shopee.vn/api/v4/item/get?itemid={itemId}&shopid={shopId}";
            var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            req.Headers.Referrer = new Uri(preflightUrl);
            req.Headers.Add("x-api-source", "pc");
            req.Headers.Add("x-shopee-language", "vi");
            req.Headers.Add("x-requested-with", "XMLHttpRequest");
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            
            var res = await _http.SendAsync(req, cancellationToken);
            if (res.IsSuccessStatusCode)
            {
                using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    var title = data.TryGetProperty("name", out var nm) ? nm.GetString() ?? string.Empty : string.Empty;
                    var priceRaw = data.TryGetProperty("price", out var pr) ? pr.GetInt64() : 0;
                    var price = NormalizeShopeePrice(priceRaw);
                    var image = data.TryGetProperty("image", out var im) ? im.GetString() ?? string.Empty : string.Empty;
                    var sold = data.TryGetProperty("historical_sold", out var hs) ? hs.GetInt32() : (data.TryGetProperty("sold", out var sd) ? sd.GetInt32() : 0);
                    var pbd = data.TryGetProperty("price_before_discount", out var pbdEl) ? pbdEl.GetInt64() : 0;
                    var rating = data.TryGetProperty("item_rating", out var ir) && ir.TryGetProperty("rating_star", out var rs) ? rs.GetDouble() : 0;
                    
                    decimal? originalPrice = null; 
                    double? discountPct = null;
                    if (pbd > 0) 
                    { 
                        var before = NormalizeShopeePrice(pbd); 
                        originalPrice = before; 
                        if (before > 0 && price > 0) discountPct = (double)((before - price) / before); 
                    }
                    
                    var thumb = string.IsNullOrWhiteSpace(image) ? string.Empty : $"https://cf.shopee.vn/file/{image}";
                    
                    if (!string.IsNullOrWhiteSpace(title) && price > 0)
                    {
                        return new ProductCandidateDto
                        {
                            Platform = Platform,
                            Title = title,
                            Price = price,
                            ShippingCost = 0,
                            ShopName = $"Shop {shopId}",
                            ShopRating = rating,
                            ShopSales = sold,
                            ProductUrl = preflightUrl,
                            ThumbnailUrl = thumb,
                            SoldCount = sold,
                            OriginalPrice = originalPrice,
                            DiscountPercent = discountPct,
                            SellerType = null
                        };
                    }
                }
            }
        }
        catch (Exception ex) 
        {
            _logger.LogWarning(ex, "Shopee API failed for {ItemId}", itemId);
        }

        // 2. Fallback to HTML parsing if API failed or returned incomplete data
        try
        {
            var html = await _http.GetStringAsync(preflightUrl, cancellationToken);
            
            // Title
            var title = string.Empty;
            var mTitle = System.Text.RegularExpressions.Regex.Match(html, @"<title>(.*?)</title>", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (mTitle.Success) 
            {
                title = System.Net.WebUtility.HtmlDecode(mTitle.Groups[1].Value);
                title = System.Text.RegularExpressions.Regex.Replace(title, @"\| Shopee.*$", string.Empty).Trim();
            }

            // Image
            var thumb = string.Empty;
            var mImg = System.Text.RegularExpressions.Regex.Match(html, "<meta property=\"og:image\" content=\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (mImg.Success) thumb = mImg.Groups[1].Value;

            decimal price = 0m;
            
            // Try JSON-LD first
            var scripts = System.Text.RegularExpressions.Regex.Matches(html, "<script type=\"application/ld\\+json\">([\\s\\S]*?)</script>", System.Text.RegularExpressions.RegexOptions.Singleline);
            foreach (System.Text.RegularExpressions.Match mx in scripts)
            {
                try
                {
                    using var jd = JsonDocument.Parse(mx.Groups[1].Value);
                    var root = jd.RootElement;
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (string.IsNullOrWhiteSpace(title)) 
                            title = root.TryGetProperty("name", out var nm2) ? nm2.GetString() ?? title : title;
                            
                        if (root.TryGetProperty("offers", out var offers))
                        {
                            if (offers.ValueKind == JsonValueKind.Object && offers.TryGetProperty("price", out var p))
                            {
                                price = p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : (decimal.TryParse(p.GetString(), out var dp) ? dp : 0m);
                            }
                        }
                    }
                }
                catch { }
            }

            // Fallback price from meta tags
            if (price == 0)
            {
                var mPrice = System.Text.RegularExpressions.Regex.Match(html, @"<meta property=""product:price:amount"" content=""([\d\.]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (mPrice.Success && decimal.TryParse(mPrice.Groups[1].Value, out var mp)) price = mp;
            }

            // If we still have no title, try og:title
            if (string.IsNullOrWhiteSpace(title))
            {
                 var mOgTitle = System.Text.RegularExpressions.Regex.Match(html, "<meta property=\"og:title\" content=\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                 if (mOgTitle.Success) title = System.Net.WebUtility.HtmlDecode(mOgTitle.Groups[1].Value);
            }

            return new ProductCandidateDto
            {
                Platform = Platform,
                Title = title,
                Price = price,
                ShippingCost = 0,
                ShopName = $"Shop {shopId}",
                ShopRating = 0,
                ShopSales = 0,
                ProductUrl = preflightUrl,
                ThumbnailUrl = thumb,
                SoldCount = 0,
                OriginalPrice = null,
                DiscountPercent = null,
                SellerType = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shopee HTML fallback failed for {Url}", preflightUrl);
        }
        
        return null;
    }

    private static decimal NormalizeShopeePrice(long raw)
    {
        if (raw <= 0) return 0m;
        // Shopee thường lưu giá theo đơn vị *100; một số API có thể trả theo *10000 hoặc *100000.
        // Quy tắc tạm thời: đưa giá về khoảng [1_000, 50_000_000] VND.
        var candidates = new[] { raw, raw / 10, raw / 100, raw / 1000, raw / 10000, raw / 100000 };
        foreach (var c in candidates)
        {
            if (c >= 1_000 && c <= 50_000_000) return (decimal)c;
        }
        return (decimal)raw; // fallback
    }
}

