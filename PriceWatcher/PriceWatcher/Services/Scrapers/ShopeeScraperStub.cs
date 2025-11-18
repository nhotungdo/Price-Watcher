using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services.Scrapers;

public class ShopeeScraperStub : IProductScraper
{
    public string Platform => "shopee";

    public Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
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

        return Task.FromResult(items);
    }
}

