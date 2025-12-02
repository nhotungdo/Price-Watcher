using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface ISearchSuggestionService
{
    Task<SearchSuggestionsResponse> GetSuggestionsAsync(string query, int? userId, int limit = 10, CancellationToken cancellationToken = default);
    Task<List<TrendingKeywordDto>> GetTrendingKeywordsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task RecordSearchAsync(string keyword, int? userId, CancellationToken cancellationToken = default);
}
