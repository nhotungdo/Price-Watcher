using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface IVisualSearchService
{
    Task<VisualSearchResponseDto> SearchByImageAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task<VisualSearchResponseDto> SearchByImageUrlAsync(string imageUrl, CancellationToken cancellationToken = default);
}
