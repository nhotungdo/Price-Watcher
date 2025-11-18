using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface ITelegramNotifier
{
    Task NotifyLoginAsync(UserLoginNotification payload, CancellationToken cancellationToken = default);
}

