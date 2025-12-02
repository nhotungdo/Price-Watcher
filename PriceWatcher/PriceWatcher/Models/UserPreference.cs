namespace PriceWatcher.Models;

public class UserPreference
{
    public int UserId { get; set; }
    public string? PreferredCategories { get; set; } // JSON array
    public string? PreferredPlatforms { get; set; } // JSON array
    public string? PriceRange { get; set; } // JSON object {min, max}
    public string? NotificationSettings { get; set; } // JSON object
    public bool EmailNotifications { get; set; } = true;
    public bool TelegramNotifications { get; set; }
    public bool PushNotifications { get; set; }
    public string? Language { get; set; } = "vi";
    public string? Currency { get; set; } = "VND";
    public DateTime? LastUpdated { get; set; }

    public string? OnboardingStatus { get; set; } = "NotStarted";
    public bool HasCompletedOnboarding { get; set; }
    public string? OnboardingDraft { get; set; }
    public string? WalletsJson { get; set; }
    public string? SavingGoalsJson { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
