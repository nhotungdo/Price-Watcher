using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PriceWatcher.Dtos;
using PriceWatcher.Options;
using PriceWatcher.Services;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Tests;

public class RecommendationServiceTests
{
    private readonly Mock<IProductScraper> _scraperA = new();
    private readonly Mock<IProductScraper> _scraperB = new();
    private readonly RecommendationService _service;

    public RecommendationServiceTests()
    {
        _scraperA.SetupGet(s => s.Platform).Returns("shopee");
        _scraperB.SetupGet(s => s.Platform).Returns("lazada");

        _scraperA.Setup(s => s.SearchByQueryAsync(It.IsAny<ProductQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProductCandidateDto { Platform = "shopee", Title = "cheap", Price = 50, ShippingCost = 5, ShopRating = 4.9, ShopSales = 200 },
                new ProductCandidateDto { Platform = "shopee", Title = "bad rating", Price = 60, ShippingCost = 5, ShopRating = 0, ShopSales = 10 },
                new ProductCandidateDto { Platform = "shopee", Title = "too cheap", Price = 5, ShippingCost = 1, ShopRating = 4.5, ShopSales = 30 }
            });

        _scraperB.Setup(s => s.SearchByQueryAsync(It.IsAny<ProductQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProductCandidateDto { Platform = "lazada", Title = "mid", Price = 55, ShippingCost = 10, ShopRating = 4.6, ShopSales = 120 },
                new ProductCandidateDto { Platform = "lazada", Title = "premium", Price = 80, ShippingCost = 0, ShopRating = 4.95, ShopSales = 500 }
            });

        var options = Microsoft.Extensions.Options.Options.Create(new RecommendationOptions
        {
            WeightPrice = 0.7m,
            WeightRating = 0.2m,
            WeightShipping = 0.1m,
            TrustedShopSalesThreshold = 100
        });

        var logger = Mock.Of<ILogger<RecommendationService>>();
        _service = new RecommendationService(new[] { _scraperA.Object, _scraperB.Object }, options, logger);
    }

    [Fact]
    public async Task RecommendAsync_FiltersBadAndReturnsTop3()
    {
        var query = new ProductQuery { Platform = "shopee", ProductId = "i.1.2", CanonicalUrl = "https://example" };

        var results = (await _service.RecommendAsync(query)).ToArray();

        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.True(r.ShopRating > 0));
        Assert.DoesNotContain(results, r => r.Title == "too cheap");
        Assert.Contains(results, r => r.Labels.Contains("BestDeal"));
    }
}

