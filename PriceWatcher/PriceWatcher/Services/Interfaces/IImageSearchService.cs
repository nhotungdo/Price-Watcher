using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface IImageSearchService
{
    Task<IEnumerable<ProductQuery>> SearchByImageAsync(Stream imageStream, CancellationToken cancellationToken = default);
}

