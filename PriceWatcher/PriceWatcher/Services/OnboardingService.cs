using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;
using System.Text.Json;

namespace PriceWatcher.Services;

public class OnboardingService : IOnboardingService
{
    private readonly PriceWatcherDbContext _dbContext;

    public OnboardingService(PriceWatcherDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserPreference> GetOrCreatePreferenceAsync(int userId, CancellationToken cancellationToken = default)
    {
        var preference = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (preference == null)
        {
            preference = new UserPreference
            {
                UserId = userId,
                OnboardingStatus = "NotStarted",
                HasCompletedOnboarding = false,
                Language = "vi",
                Currency = "VND",
                LastUpdated = DateTime.UtcNow
            };
            _dbContext.UserPreferences.Add(preference);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        return preference;
    }

    public async Task<string> GetStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        return preference.OnboardingStatus ?? "NotStarted";
    }

    public async Task SetStatusAsync(int userId, string status, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        preference.OnboardingStatus = status;
        preference.LastUpdated = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveBasicSettingsAsync(int userId, string currency, string language, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        preference.Currency = currency;
        preference.Language = language;
        preference.LastUpdated = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveFirstWalletAsync(int userId, string name, string type, decimal initialBalance, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        var wallets = new List<object>
        {
            new { Name = name, Type = type, Balance = initialBalance, CreatedAt = DateTime.UtcNow }
        };
        preference.WalletsJson = JsonSerializer.Serialize(wallets);
        preference.LastUpdated = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveCategoriesAsync(int userId, IEnumerable<string> categories, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        preference.PreferredCategories = JsonSerializer.Serialize(categories);
        preference.LastUpdated = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(decimal monthlyAmount, int months)> SaveSavingGoalAsync(int userId, string goalName, decimal targetAmount, DateTime deadline, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        var months = Math.Max(1, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalDays / 30));
        var monthlyAmount = targetAmount / months;

        var goals = new List<object>
        {
            new { Name = goalName, TargetAmount = targetAmount, Deadline = deadline, MonthlyAmount = monthlyAmount, CreatedAt = DateTime.UtcNow }
        };
        preference.SavingGoalsJson = JsonSerializer.Serialize(goals);
        preference.LastUpdated = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (monthlyAmount, months);
    }

    public async Task CompleteAsync(int userId, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        preference.HasCompletedOnboarding = true;
        preference.OnboardingStatus = "Completed";
        preference.LastUpdated = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
