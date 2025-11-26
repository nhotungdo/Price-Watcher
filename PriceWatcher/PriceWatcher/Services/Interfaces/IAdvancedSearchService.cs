using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface IAdvancedSearchService
{
    Task<SearchResult> SearchWithFiltersAsync(SearchFilters filters, CancellationToken cancellationToken = default);
    Task<List<string>> GetSearchSuggestionsAsync(string keyword, int limit = 10, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetCategoryCountsAsync(string? keyword = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetPlatformCountsAsync(string? keyword = null, CancellationToken cancellationToken = default);
    Task<(decimal? min, decimal? max)> GetPriceRangeAsync(string? keyword = null, CancellationToken cancellationToken = default);
}
