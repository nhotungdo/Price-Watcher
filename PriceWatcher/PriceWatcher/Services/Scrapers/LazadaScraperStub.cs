using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services.Scrapers;

public class LazadaScraperStub : IProductScraper
{
    public string Platform => "lazada";

    public Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
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

        return Task.FromResult(items);
    }
}

