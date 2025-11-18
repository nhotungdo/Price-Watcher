using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface IRecommendationService
{
    Task<IEnumerable<ProductCandidateDto>> RecommendAsync(ProductQuery query, int top = 3, CancellationToken cancellationToken = default);
}

