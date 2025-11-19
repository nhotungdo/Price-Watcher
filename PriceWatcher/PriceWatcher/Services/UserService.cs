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
    private readonly IEmailSender _emailSender;

    public UserService(PriceWatcherDbContext dbContext, ITelegramNotifier telegramNotifier, ILogger<UserService> logger, IEmailSender emailSender)
    {
        _dbContext = dbContext;
        _telegramNotifier = telegramNotifier;
        _logger = logger;
        _emailSender = emailSender;
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
        if (user.CreatedAt.HasValue && (DateTime.UtcNow - user.CreatedAt.Value).TotalSeconds < 5)
        {
            try
            {
                await _emailSender.SendRegistrationConfirmationAsync(user.Email, user.FullName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send registration email to {Email}", user.Email);
            }
        }
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User> RegisterLocalAsync(string email, string? fullName, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var user = new User
        {
            Email = email,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateLastLoginAsync(User user, CancellationToken cancellationToken = default)
    {
        user.LastLogin = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
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

