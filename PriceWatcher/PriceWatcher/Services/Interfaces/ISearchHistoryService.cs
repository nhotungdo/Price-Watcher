using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface ISearchHistoryService
{
    Task SaveSearchHistoryAsync(Guid searchId, int userId, string searchType, string inputContent, ProductQuery query, IEnumerable<ProductCandidateDto> results, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SearchHistoryDto>> GetUserHistoryAsync(int userId, int page, int pageSize, string? keyword = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteHistoryAsync(int userId, int historyId, CancellationToken cancellationToken = default);
}

