using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

/// <summary>
/// Core service for product tracking and price history management
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Track a product by URL - creates or updates product and price history
    /// </summary>
    /// <param name="url">Product URL from Shopee, Lazada, or Tiki</param>
    /// <param name="userId">Current user ID (optional for anonymous tracking)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details with price history</returns>
    Task<ProductTrackingResultDto> TrackProductByUrlAsync(
        string url, 
        int? userId = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get product details with price history
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product with price history chart data</returns>
    Task<ProductWithHistoryDto?> GetProductWithHistoryAsync(
        int productId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if product URL exists in database
    /// </summary>
    /// <param name="url">Product URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product ID if exists, null otherwise</returns>
    Task<int?> FindProductByUrlAsync(
        string url, 
        CancellationToken cancellationToken = default);
}
