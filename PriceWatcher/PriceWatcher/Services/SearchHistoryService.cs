using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class SearchHistoryService : ISearchHistoryService
{
    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILogger<SearchHistoryService> _logger;
    private const int HistoryLimit = 50;

    public SearchHistoryService(PriceWatcherDbContext dbContext, ILogger<SearchHistoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SaveSearchHistoryAsync(Guid searchId, int userId, string searchType, string inputContent, ProductQuery query, IEnumerable<ProductCandidateDto> results, CancellationToken cancellationToken = default)
    {
        var topResult = results.FirstOrDefault();

        var history = new SearchHistory
        {
            UserId = userId,
            SearchType = searchType,
            InputContent = inputContent,
            DetectedKeyword = query.TitleHint,
            BestPriceFound = topResult?.TotalCost,
            SearchTime = DateTime.UtcNow
        };

        _dbContext.SearchHistories.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await EnforceHistoryLimitAsync(userId, cancellationToken);

        _logger.LogInformation("Saved search history {SearchId} for user {UserId}", searchId, userId);
    }

    public async Task<IReadOnlyCollection<SearchHistoryDto>> GetUserHistoryAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SearchHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.SearchTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var items = await query
            .Select(h => new SearchHistoryDto
            {
                HistoryId = h.HistoryId,
                SearchType = h.SearchType,
                InputContent = h.InputContent,
                DetectedKeyword = h.DetectedKeyword,
                BestPriceFound = h.BestPriceFound,
                SearchTime = h.SearchTime
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<bool> DeleteHistoryAsync(int userId, int historyId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SearchHistories
            .FirstOrDefaultAsync(h => h.HistoryId == historyId && h.UserId == userId, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        _dbContext.SearchHistories.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnforceHistoryLimitAsync(int userId, CancellationToken cancellationToken)
    {
        var total = await _dbContext.SearchHistories.CountAsync(h => h.UserId == userId, cancellationToken);
        if (total <= HistoryLimit)
        {
            return;
        }

        var toRemove = await _dbContext.SearchHistories
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.SearchTime)
            .Take(total - HistoryLimit)
            .ToListAsync(cancellationToken);

        _dbContext.SearchHistories.RemoveRange(toRemove);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

