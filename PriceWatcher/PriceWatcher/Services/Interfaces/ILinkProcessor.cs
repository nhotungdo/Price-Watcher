using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface ILinkProcessor
{
    Task<ProductQuery> ProcessUrlAsync(string url, CancellationToken cancellationToken = default);
}

