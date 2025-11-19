using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services.Scrapers;

public class TikiScraperStub : IProductScraper
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _rate = new(5);
    private readonly IMetricsService _metrics;

    public TikiScraperStub(IHttpClientFactory httpFactory, IMetricsService metrics)
    {
        _http = httpFactory.CreateClient("tiki");
        _metrics = metrics;
    }

    public string Platform => "tiki";

    public async Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _rate.WaitAsync(cancellationToken);
        try
        {
            var rng = new Random(query.ProductId.GetHashCode() + 20);
            var items = Enumerable.Range(1, 5).Select(i => new ProductCandidateDto
            {
                Platform = Platform,
                Title = $"{query.TitleHint ?? "Tiki Item"} #{i}",
                Price = 90_000 + rng.Next(1_000, 9_000),
                ShippingCost = rng.Next(0, 12_000),
                ShopName = $"Tiki Store {i}",
                ShopRating = 4.3 + rng.NextDouble() / 3,
                ShopSales = rng.Next(5, 300),
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

