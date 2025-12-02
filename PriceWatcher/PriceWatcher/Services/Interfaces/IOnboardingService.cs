using PriceWatcher.Models;

namespace PriceWatcher.Services.Interfaces;

public interface IOnboardingService
{
    Task<UserPreference> GetOrCreatePreferenceAsync(int userId, CancellationToken cancellationToken = default);
    Task<string> GetStatusAsync(int userId, CancellationToken cancellationToken = default);
    Task SetStatusAsync(int userId, string status, CancellationToken cancellationToken = default);
    Task SaveBasicSettingsAsync(int userId, string currency, string language, CancellationToken cancellationToken = default);
    Task SaveFirstWalletAsync(int userId, string name, string type, decimal initialBalance, CancellationToken cancellationToken = default);
    Task SaveCategoriesAsync(int userId, IEnumerable<string> categories, CancellationToken cancellationToken = default);
    Task<(decimal monthlyAmount, int months)> SaveSavingGoalAsync(int userId, string goalName, decimal targetAmount, DateTime deadline, CancellationToken cancellationToken = default);
    Task CompleteAsync(int userId, CancellationToken cancellationToken = default);
}
