using System.Text.Json;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services.Scrapers;

public class TikiScraperStub : IProductScraper
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _rate = new(5);
    private readonly IMetricsService _metrics;
    private readonly ILogger<TikiScraperStub> _logger;

    public TikiScraperStub(IHttpClientFactory httpFactory, IMetricsService metrics, ILogger<TikiScraperStub> logger)
    {
        _http = httpFactory.CreateClient("tiki");
        _metrics = metrics;
        _logger = logger;
    }

    public string Platform => "tiki";

    public async Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _rate.WaitAsync(cancellationToken);
        try
        {
            var keyword = string.IsNullOrWhiteSpace(query.TitleHint) ? query.ProductId : query.TitleHint!;
            var list = new List<ProductCandidateDto>();
            // cố gắng API trước
            var url = $"https://api.tiki.vn/raiden/v2/products?q={Uri.EscapeDataString(keyword)}&page=1&limit=20";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Referrer = new Uri("https://tiki.vn");
            var res = await _http.SendAsync(req, cancellationToken);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStreamAsync(cancellationToken);
                var doc = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        var title = item.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                        var price = item.TryGetProperty("price", out var p) ? p.GetDecimal() : 0m;
                        var rating = item.TryGetProperty("rating_average", out var r) ? (double)(r.GetDouble()) : 0;
                        var reviews = item.TryGetProperty("review_count", out var rc) ? rc.GetInt32() : 0;
                        var thumb = item.TryGetProperty("thumbnail_url", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                        var urlPath = item.TryGetProperty("url_path", out var up) ? up.GetString() ?? string.Empty : string.Empty;
                        var sellerName = item.TryGetProperty("seller", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() ?? string.Empty : string.Empty;
                        var isOfficial = item.TryGetProperty("seller", out var s2) && s2.TryGetProperty("is_official", out var io) ? io.GetBoolean() : false;
                        var sold = item.TryGetProperty("quantity_sold", out var qs) && qs.ValueKind == JsonValueKind.Object && qs.TryGetProperty("value", out var qsv) ? qsv.GetInt32() : (item.TryGetProperty("quantity_sold", out var qs2) && qs2.ValueKind == JsonValueKind.Number ? qs2.GetInt32() : 0);
                        var listPrice = item.TryGetProperty("list_price", out var lp) && lp.ValueKind == JsonValueKind.Number ? lp.GetDecimal() : 0m;
                        double? discountPct = null;
                        decimal? originalPrice = null;
                        if (listPrice > 0m)
                        {
                            originalPrice = listPrice;
                            if (price > 0m) discountPct = (double)((listPrice - price) / listPrice);
                        }
                        var productUrl = string.IsNullOrWhiteSpace(urlPath) ? query.CanonicalUrl : $"https://tiki.vn/{urlPath}";
                        list.Add(new ProductCandidateDto
                        {
                            Platform = Platform,
                            Title = title,
                            Price = price,
                            ShippingCost = 0,
                            ShopName = string.IsNullOrWhiteSpace(sellerName) ? "Tiki" : sellerName,
                            ShopRating = rating,
                            ShopSales = reviews,
                            ProductUrl = productUrl,
                            ThumbnailUrl = thumb,
                            SoldCount = sold,
                            OriginalPrice = originalPrice,
                            DiscountPercent = discountPct,
                            SellerType = isOfficial ? "Chính hãng" : "Đại lý"
                        });
                    }
                }
            }
            else
            {
                var snippet = await res.Content.ReadAsStringAsync(cancellationToken);
                if (snippet.Length > 256) snippet = snippet[..256];
                _logger.LogWarning("Tiki API returned {Status} for {Keyword}. Snippet={Snippet}", res.StatusCode, keyword, snippet);
                // fallback HTML: parse __NEXT_DATA__
                var searchUrl = $"https://tiki.vn/search?q={Uri.EscapeDataString(keyword)}";
                var html = await _http.GetStringAsync(searchUrl, cancellationToken);
                var m = System.Text.RegularExpressions.Regex.Match(html, @"id=""__NEXT_DATA__""[^>]*>([\s\S]*?)</script>", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (m.Success)
                {
                    using var nextDoc = JsonDocument.Parse(m.Groups[1].Value);
                    // cố gắng tìm mảng sản phẩm trong cây dữ liệu phổ biến
                    ExtractTikiProductsFromNextData(nextDoc.RootElement, list, query);
                }
            }
            _logger.LogInformation("Tiki returned {Count} items for {Keyword}", list.Count, keyword);
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
        var m = System.Text.RegularExpressions.Regex.Match(html, @"id=""__NEXT_DATA__""[^>]*>([\s\S]*?)</script>", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (!m.Success) return null;
        using var doc = JsonDocument.Parse(m.Groups[1].Value);
        var list = new List<ProductCandidateDto>();
        ExtractTikiProductsFromNextData(doc.RootElement, list, query);
        return list.FirstOrDefault();
    }

    private static void ExtractTikiProductsFromNextData(JsonElement root, List<ProductCandidateDto> list, ProductQuery query)
    {
        foreach (var prop in root.EnumerateObject())
        {
            var el = prop.Value;
            if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in el.EnumerateArray())
                {
                    TryAddTikiItem(item, list, query);
                }
            }
            else if (el.ValueKind == JsonValueKind.Object)
            {
                TryAddTikiItem(el, list, query);
                ExtractTikiProductsFromNextData(el, list, query);
            }
        }
    }

    private static void TryAddTikiItem(JsonElement item, List<ProductCandidateDto> list, ProductQuery query)
    {
        if (item.ValueKind != JsonValueKind.Object) return;
        var hasName = item.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String;
        var hasPrice = item.TryGetProperty("price", out var p) && (p.ValueKind == JsonValueKind.Number || p.ValueKind == JsonValueKind.String);
        if (!hasName || !hasPrice) return;
        var title = n.GetString() ?? string.Empty;
        decimal price = 0m;
        if (item.TryGetProperty("price", out var p2))
        {
            if (p2.ValueKind == JsonValueKind.Number) price = p2.GetDecimal();
            else if (p2.ValueKind == JsonValueKind.String && decimal.TryParse(p2.GetString(), out var dp)) price = dp;
        }
        var rating = item.TryGetProperty("rating_average", out var r) && r.ValueKind == JsonValueKind.Number ? r.GetDouble() : 0;
        var reviews = item.TryGetProperty("review_count", out var rc) && rc.ValueKind == JsonValueKind.Number ? rc.GetInt32() : 0;
        var thumb = item.TryGetProperty("thumbnail_url", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() ?? string.Empty : string.Empty;
        var urlPath = item.TryGetProperty("url_path", out var up) && up.ValueKind == JsonValueKind.String ? up.GetString() ?? string.Empty : string.Empty;
        var sellerName = item.TryGetProperty("seller", out var s) && s.ValueKind == JsonValueKind.Object && s.TryGetProperty("name", out var sn) ? sn.GetString() ?? string.Empty : string.Empty;
        var productUrl = string.IsNullOrWhiteSpace(urlPath) ? query.CanonicalUrl : $"https://tiki.vn/{urlPath}";
        list.Add(new ProductCandidateDto
        {
            Platform = "tiki",
            Title = title,
            Price = price,
            ShippingCost = 0,
            ShopName = string.IsNullOrWhiteSpace(sellerName) ? "Tiki" : sellerName,
            ShopRating = rating,
            ShopSales = reviews,
            ProductUrl = productUrl,
            ThumbnailUrl = thumb
        });
    }
}

