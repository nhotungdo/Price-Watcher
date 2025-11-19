using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services.Scrapers;

public class LazadaScraperStub : IProductScraper
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _rate = new(5);
    private readonly IMetricsService _metrics;

    public LazadaScraperStub(IHttpClientFactory httpFactory, IMetricsService metrics)
    {
        _http = httpFactory.CreateClient("lazada");
        _metrics = metrics;
    }

    public string Platform => "lazada";

    public async Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _rate.WaitAsync(cancellationToken);
        try
        {
            var rng = new Random(query.ProductId.GetHashCode() + 10);
            var items = Enumerable.Range(1, 5).Select(i => new ProductCandidateDto
            {
                Platform = Platform,
                Title = $"{query.TitleHint ?? "Lazada Item"} #{i}",
                Price = 120_000 + rng.Next(1_000, 12_000),
                ShippingCost = rng.Next(5_000, 25_000),
                ShopName = $"Lazada Mall {i}",
                ShopRating = 4.2 + rng.NextDouble() / 2,
                ShopSales = rng.Next(20, 800),
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

