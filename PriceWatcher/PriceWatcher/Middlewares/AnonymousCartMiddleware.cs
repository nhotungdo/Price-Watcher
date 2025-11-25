using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Middlewares;

public class AnonymousCartMiddleware
{
    private readonly RequestDelegate _next;

    public AnonymousCartMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var cartSession = context.RequestServices.GetRequiredService<ICartSessionService>();
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            cartSession.ClearAnonymousCookie(context);
        }
        else
        {
            cartSession.GetAnonymousId(context, createIfMissing: true);
        }

        await _next(context);
    }
}

