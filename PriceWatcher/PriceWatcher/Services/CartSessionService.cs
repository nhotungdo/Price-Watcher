using Microsoft.Extensions.Logging;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class CartSessionService : ICartSessionService
{
    public const string AnonymousCartCookie = "pw_anonymous_id";
    private readonly ICartService _cartService;
    private readonly ILogger<CartSessionService> _logger;

    public CartSessionService(ICartService cartService, ILogger<CartSessionService> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    public Guid EnsureAnonymousId(HttpContext context)
    {
        var existing = GetAnonymousId(context, createIfMissing: false);
        if (existing.HasValue)
        {
            return existing.Value;
        }

        return GetAnonymousId(context, createIfMissing: true)!.Value;
    }

    public Guid? GetAnonymousId(HttpContext context, bool createIfMissing = false)
    {
        if (context.Request.Cookies.TryGetValue(AnonymousCartCookie, out var cookie) &&
            Guid.TryParse(cookie, out var value))
        {
            context.Items[AnonymousCartCookie] = value;
            return value;
        }

        if (!createIfMissing)
        {
            return null;
        }

        var newId = Guid.NewGuid();
        context.Items[AnonymousCartCookie] = newId;
        context.Response.Cookies.Append(AnonymousCartCookie, newId.ToString(), BuildCookieOptions(context));
        return newId;
    }

    public void ClearAnonymousCookie(HttpContext context)
    {
        if (context.Request.Cookies.ContainsKey(AnonymousCartCookie))
        {
            context.Response.Cookies.Delete(AnonymousCartCookie);
        }
        context.Items.Remove(AnonymousCartCookie);
    }

    public async Task MergeOnLoginAsync(HttpContext context, int userId, CancellationToken cancellationToken = default)
    {
        var anonymousId = GetAnonymousId(context);
        if (!anonymousId.HasValue)
        {
            return;
        }

        try
        {
            await _cartService.MergeAsync(userId, anonymousId.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to merge anonymous cart for user {UserId}", userId);
        }
        finally
        {
            ClearAnonymousCookie(context);
        }
    }

    public void HandleLogout(HttpContext context)
    {
        ClearAnonymousCookie(context);
        GetAnonymousId(context, createIfMissing: true);
    }

    private static CookieOptions BuildCookieOptions(HttpContext context)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
    }
}

