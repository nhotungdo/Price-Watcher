using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services;
using PriceWatcher.Services.Interfaces;
using Xunit;

namespace PriceWatcher.Tests;

public class ProductSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithKeyword_ReturnsRankedResults()
    {
        await using var context = BuildContext();
        SeedProducts(context);
        var service = BuildService(context);

        var response = await service.SearchAsync("iPhone", 1, 8);

        Assert.Equal("keyword", response.SearchMode);
        Assert.True(response.TotalItems >= 2);
        Assert.All(response.Items, item => Assert.Contains("iphone", item.Title, StringComparison.OrdinalIgnoreCase));
        Assert.True(response.DurationMs >= 0);
    }

    [Fact]
    public async Task SearchAsync_WithSupportedUrl_ReturnsExactProduct()
    {
        await using var context = BuildContext();
        SeedProducts(context);
        var service = BuildService(context);
        var url = "https://tiki.vn/airpods-pro-gen-2-p12345.html?spid=987";

        var response = await service.SearchAsync(url, 1, 10);

        Assert.Equal("url", response.SearchMode);
        Assert.Single(response.Items);
        Assert.Equal("AirPods Pro Gen 2", response.Items[0].Title);
    }

    [Fact]
    public async Task SuggestAsync_ReturnsTopNames()
    {
        await using var context = BuildContext();
        SeedProducts(context);
        var service = BuildService(context);

        var suggestions = await service.SuggestAsync("air", 5);

        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.name.Contains("AirPods", StringComparison.OrdinalIgnoreCase));
    }

    private static PriceWatcherDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<PriceWatcherDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PriceWatcherDbContext(options);
    }

    private static ProductSearchService BuildService(PriceWatcherDbContext context)
    {
        var linkProcessor = new LinkProcessor();
        var recommendation = new FakeRecommendationService();
        return new ProductSearchService(context, linkProcessor, recommendation, NullLogger<ProductSearchService>.Instance);
    }

    private static void SeedProducts(PriceWatcherDbContext context)
    {
        context.Platforms.Add(new Platform { PlatformId = 1, PlatformName = "tiki" });
        context.Products.AddRange(
            new Product
            {
                ProductId = 1,
                PlatformId = 1,
                ProductName = "iPhone 15 Pro Max 256GB",
                OriginalUrl = "https://tiki.vn/iphone-15-pro-max-p111.html",
                CurrentPrice = 32990000,
                Rating = 4.9,
                ReviewCount = 2500,
                LastUpdated = DateTime.UtcNow
            },
            new Product
            {
                ProductId = 2,
                PlatformId = 1,
                ProductName = "AirPods Pro Gen 2",
                OriginalUrl = "https://tiki.vn/airpods-pro-gen-2-p12345.html",
                CurrentPrice = 5290000,
                Rating = 4.8,
                ReviewCount = 800,
                LastUpdated = DateTime.UtcNow
            },
            new Product
            {
                ProductId = 3,
                PlatformId = 1,
                ProductName = "Ốp lưng iPhone 15 chống sốc",
                OriginalUrl = "https://tiki.vn/op-lung-iphone-15-chong-soc-p555.html",
                CurrentPrice = 199000,
                Rating = 4.5,
                ReviewCount = 120,
                LastUpdated = DateTime.UtcNow
            });
        context.SaveChanges();
    }

    private sealed class FakeRecommendationService : IRecommendationService
    {
        public Task<IEnumerable<ProductCandidateDto>> RecommendAsync(ProductQuery query, int top = 3, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<ProductCandidateDto>>(Array.Empty<ProductCandidateDto>());
    }
}

