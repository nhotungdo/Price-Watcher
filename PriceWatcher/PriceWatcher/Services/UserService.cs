using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class UserService : IUserService
{
    private readonly PriceWatcherDbContext _dbContext;
    private readonly ITelegramNotifier _telegramNotifier;
    private readonly ILogger<UserService> _logger;

    public UserService(PriceWatcherDbContext dbContext, ITelegramNotifier telegramNotifier, ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _telegramNotifier = telegramNotifier;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserFromGoogleAsync(GoogleUserInfo info, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == info.Email, cancellationToken);
        if (user == null)
        {
            user = new User
            {
                Email = info.Email,
                FullName = info.Name,
                AvatarUrl = info.AvatarUrl,
                GoogleId = info.GoogleId,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
        }
        else
        {
            user.FullName = info.Name ?? user.FullName;
            user.AvatarUrl = info.AvatarUrl ?? user.AvatarUrl;
            user.GoogleId = info.GoogleId ?? user.GoogleId;
            user.LastLogin = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task OnLoginSuccessAsync(User user, HttpRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new UserLoginNotification
        {
            Email = user.Email,
            IpAddress = request.HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = request.Headers.UserAgent.ToString(),
            TimestampUtc = DateTime.UtcNow
        };

        try
        {
            await _telegramNotifier.NotifyLoginAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify Telegram for user {Email}", user.Email);
        }
    }
}

