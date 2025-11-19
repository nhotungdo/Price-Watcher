using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services.Scrapers;

public class ShopeeScraperStub : IProductScraper
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _rate = new(5);
    private readonly IMetricsService _metrics;

    public ShopeeScraperStub(IHttpClientFactory httpFactory, IMetricsService metrics)
    {
        _http = httpFactory.CreateClient("shopee");
        _metrics = metrics;
    }

    public string Platform => "shopee";

    public async Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _rate.WaitAsync(cancellationToken);
        try
        {
            var rng = new Random(query.ProductId.GetHashCode());
            var items = Enumerable.Range(1, 5).Select(i => new ProductCandidateDto
            {
                Platform = Platform,
                Title = $"{query.TitleHint ?? "Shopee Item"} #{i}",
                Price = 100_000 + rng.Next(1_000, 10_000),
                ShippingCost = rng.Next(0, 30_000),
                ShopName = $"Shopee Shop {i}",
                ShopRating = 4 + rng.NextDouble(),
                ShopSales = rng.Next(10, 500),
                ProductUrl = query.CanonicalUrl,
                ThumbnailUrl = "https://via.placeholder.com/150"
            });
            return items;
        }
        finally
        {
            _rate.Release();
            sw.Stop();
            _metrics.RecordScraperCall(Platform, success: true, elapsedMs: sw.ElapsedMilliseconds);
        }
    }
}

