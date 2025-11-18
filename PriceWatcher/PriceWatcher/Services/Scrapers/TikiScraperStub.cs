using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services.Scrapers;

public class TikiScraperStub : IProductScraper
{
    public string Platform => "tiki";

    public Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
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

        return Task.FromResult(items);
    }
}

