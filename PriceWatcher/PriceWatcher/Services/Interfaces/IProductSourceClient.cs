using System.Collections.Generic;

namespace PriceWatcher.Services.Interfaces;

public interface IProductSourceClient
{
    Task<PriceWatcher.Dtos.ProductDto?> GetProductByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceWatcher.Dtos.ProductDto>> SearchByTitleAsync(string title, SearchOptions options, CancellationToken cancellationToken = default);
}

public class SearchOptions
{
    public IEnumerable<string>? Platforms { get; set; }
}
