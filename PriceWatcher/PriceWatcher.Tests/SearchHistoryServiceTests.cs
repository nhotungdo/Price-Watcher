using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services;

namespace PriceWatcher.Tests;

public class SearchHistoryServiceTests
{
    [Fact]
    public async Task SaveSearchHistoryAsync_EnforcesLimit()
    {
        var options = new DbContextOptionsBuilder<PriceWatcherDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new TestDbContext(options);
        for (var i = 0; i < 52; i++)
        {
            context.SearchHistories.Add(new SearchHistory
            {
                UserId = 1,
                SearchType = "url",
                InputContent = $"seed-{i}",
                SearchTime = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await context.SaveChangesAsync();

        var service = new SearchHistoryService(context, Mock.Of<ILogger<SearchHistoryService>>());

        await service.SaveSearchHistoryAsync(
            Guid.NewGuid(),
            1,
            "url",
            "latest",
            new ProductQuery { CanonicalUrl = "https://example", Platform = "shopee", ProductId = "i.1.2" },
            new[]
            {
                new ProductCandidateDto { Platform = "shopee", Price = 10, ShippingCost = 2, ShopRating = 4.5 }
            });

        var histories = await context.SearchHistories.Where(h => h.UserId == 1).OrderBy(h => h.SearchTime).ToListAsync();

        Assert.Equal(50, histories.Count);
        Assert.DoesNotContain(histories, h => h.InputContent == "seed-49");
        Assert.DoesNotContain(histories, h => h.InputContent == "seed-50");
        Assert.DoesNotContain(histories, h => h.InputContent == "seed-51");
        Assert.Contains(histories, h => h.InputContent == "latest");
    }
}

file class TestDbContext : PriceWatcherDbContext
{
    public TestDbContext(DbContextOptions<PriceWatcherDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Prevent base SQL Server configuration so we can use in-memory provider
    }
}

