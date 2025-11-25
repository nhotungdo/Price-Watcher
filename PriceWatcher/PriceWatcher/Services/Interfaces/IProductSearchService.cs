using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface IProductSearchService
{
    Task<ProductSearchResponse> SearchAsync(string input, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(int? productId, string name)>> SuggestAsync(string keyword, int limit, CancellationToken cancellationToken = default);
}

