using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

/// <summary>
/// Service for searching products across multiple e-commerce platforms
/// </summary>
public interface IMultiPlatformSearchService
{
    /// <summary>
    /// Search for products across multiple platforms (Shopee, Lazada, Tiki)
    /// </summary>
    Task<MultiPlatformSearchResponse> SearchAsync(
        MultiPlatformSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare prices for similar products across platforms
    /// </summary>
    Task<List<PriceComparisonDto>> ComparePricesAsync(
        string keyword,
        CancellationToken cancellationToken = default);
}
