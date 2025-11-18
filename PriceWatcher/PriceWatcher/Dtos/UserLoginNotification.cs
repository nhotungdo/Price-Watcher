namespace PriceWatcher.Dtos;

public class UserLoginNotification
{
    public string Email { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

