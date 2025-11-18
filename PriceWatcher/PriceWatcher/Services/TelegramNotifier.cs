using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceWatcher.Dtos;
using PriceWatcher.Options;
using PriceWatcher.Services.Interfaces;
using Telegram.Bot;

namespace PriceWatcher.Services;

public class TelegramNotifier : ITelegramNotifier
{
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(ITelegramBotClient botClient, IOptions<TelegramOptions> options, ILogger<TelegramNotifier> logger)
    {
        _botClient = botClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyLoginAsync(UserLoginNotification payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AdminChatId))
        {
            _logger.LogWarning("Telegram AdminChatId not configured");
            return;
        }

        var message = $"🔔 *New Login*\n" +
                      $"Email: {payload.Email}\n" +
                      $"IP: {payload.IpAddress ?? "unknown"}\n" +
                      $"Agent: {payload.UserAgent ?? "unknown"}\n" +
                      $"Time (UTC): {payload.TimestampUtc:O}";

        try
        {
            if (!long.TryParse(_options.AdminChatId, out var chatId))
            {
                _logger.LogError("Invalid Telegram chat id configuration");
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram notification");
        }
    }
}

