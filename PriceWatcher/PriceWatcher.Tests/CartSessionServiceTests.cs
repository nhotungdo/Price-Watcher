using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PriceWatcher.Services;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Tests;

public class CartSessionServiceTests
{
    [Fact]
    public void EnsureAnonymousId_SetsCookieWhenMissing()
    {
        var cartService = new Mock<ICartService>();
        var sessionService = new CartSessionService(cartService.Object, NullLogger<CartSessionService>.Instance);
        var context = new DefaultHttpContext();

        var id = sessionService.EnsureAnonymousId(context);

        Assert.NotEqual(Guid.Empty, id);
        Assert.Contains(CartSessionService.AnonymousCartCookie, context.Response.Headers["Set-Cookie"]);
    }

    [Fact]
    public async Task MergeOnLoginAsync_MergesAndClearsCookie()
    {
        var anonId = Guid.NewGuid();
        var cartService = new Mock<ICartService>();
        var sessionService = new CartSessionService(cartService.Object, NullLogger<CartSessionService>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", $"{CartSessionService.AnonymousCartCookie}={anonId}");

        await sessionService.MergeOnLoginAsync(context, 42);

        cartService.Verify(s => s.MergeAsync(42, anonId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains($"{CartSessionService.AnonymousCartCookie}=;", context.Response.Headers["Set-Cookie"]);
    }
}

