using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services;
using Xunit;

namespace PriceWatcher.Tests;

public class CartServiceTests
{
    [Fact]
    public async Task AddItemAsync_CreatesAnonymousCart()
    {
        await using var context = BuildContext();
        var service = new CartService(context, NullLogger<CartService>.Instance);
        var anonymousId = Guid.NewGuid();

        var response = await service.AddItemAsync(new AddCartItemRequest
        {
            ProductId = 10,
            Name = "Test Product",
            PlatformId = 1,
            PlatformName = "Tiki",
            Price = 100000,
            Quantity = 2
        }, null, anonymousId);

        Assert.Single(response.Items);
        Assert.Equal(2, response.Summary.ItemCount);

        var cart = await context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.AnonymousId == anonymousId);
        Assert.NotNull(cart);
        Assert.Equal(2, cart!.Items.First().Quantity);
    }

    [Fact]
    public async Task MergeAsync_MovesItemsIntoUserCart()
    {
        await using var context = BuildContext();
        var service = new CartService(context, NullLogger<CartService>.Instance);
        var anonymousId = Guid.NewGuid();

        await service.AddItemAsync(new AddCartItemRequest
        {
            ProductId = 1,
            Name = "Anonymous item",
            Price = 50000,
            Quantity = 1
        }, null, anonymousId);

        var user = new User { Email = "test@example.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await service.MergeAsync(user.UserId, anonymousId);
        var userCart = await context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == user.UserId);

        Assert.NotNull(userCart);
        Assert.Single(userCart!.Items);
        Assert.Equal("Anonymous item", userCart.Items.First().ProductName);

        var anonCart = await context.Carts.FirstOrDefaultAsync(c => c.AnonymousId == anonymousId);
        Assert.Null(anonCart);
    }

    [Fact]
    public async Task UpdateQuantityAsync_RespectsLimits()
    {
        await using var context = BuildContext();
        var service = new CartService(context, NullLogger<CartService>.Instance);
        var anonymousId = Guid.NewGuid();

        await service.AddItemAsync(new AddCartItemRequest
        {
            ProductId = 2,
            Name = "Limited Item",
            Price = 123000,
            Quantity = 1
        }, null, anonymousId);

        var updated = await service.UpdateQuantityAsync(2, null, 5, null, anonymousId);
        Assert.Equal(5, updated.Items.First().Quantity);
    }

    private static PriceWatcherDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<PriceWatcherDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PriceWatcherDbContext(options);
    }
}

