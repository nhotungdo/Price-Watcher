using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface IProductScraper
{
    string Platform { get; }

    Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(ProductQuery query, CancellationToken cancellationToken = default);
}

