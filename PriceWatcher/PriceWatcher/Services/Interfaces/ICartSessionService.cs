namespace PriceWatcher.Services.Interfaces;

public interface ICartSessionService
{
    Guid EnsureAnonymousId(HttpContext context);
    Guid? GetAnonymousId(HttpContext context, bool createIfMissing = false);
    void ClearAnonymousCookie(HttpContext context);
    Task MergeOnLoginAsync(HttpContext context, int userId, CancellationToken cancellationToken = default);
    void HandleLogout(HttpContext context);
}

