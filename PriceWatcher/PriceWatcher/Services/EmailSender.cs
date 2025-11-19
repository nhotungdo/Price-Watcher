using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceWatcher.Options;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class EmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendRegistrationConfirmationAsync(string toEmail, string? fullName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogWarning("Email options not configured. Skipping registration email for {Email}", toEmail);
            return;
        }

        using var client = new SmtpClient(_options.Host!, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        var from = new MailAddress(_options.FromEmail!, _options.FromName ?? "PriceWatcher");
        var to = new MailAddress(toEmail, fullName ?? toEmail);
        using var message = new MailMessage(from, to)
        {
            Subject = "Xác nhận đăng ký PriceWatcher",
            Body = $"Chào {fullName ?? toEmail},\n\nTài khoản của bạn đã được tạo thành công bằng Google.\n\nCảm ơn bạn đã sử dụng PriceWatcher!",
            IsBodyHtml = false
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}