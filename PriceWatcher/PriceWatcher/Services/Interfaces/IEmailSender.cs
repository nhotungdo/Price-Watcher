using System.Threading.Tasks;

namespace PriceWatcher.Services.Interfaces;

public interface IEmailSender
{
    Task SendRegistrationConfirmationAsync(string toEmail, string? fullName, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string to, string subject, string htmlBody);
}