using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface ISuggestedProductsService
{
    /// <summary>
    /// Gets suggested products from all platforms (Tiki, Shopee, Lazada)
    /// </summary>
    /// <param name="limit">Maximum number of products to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suggested products from all platforms</returns>
    Task<List<SuggestedProductDto>> GetSuggestedProductsAsync(int limit = 12, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets suggested products from a specific platform
    /// </summary>
    /// <param name="platformName">Platform name (Tiki, Shopee, or Lazada)</param>
    /// <param name="limit">Maximum number of products to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suggested products from the specified platform</returns>
    Task<List<SuggestedProductDto>> GetSuggestedProductsByPlatformAsync(string platformName, int limit = 4, CancellationToken cancellationToken = default);

    Task<List<SuggestedProductDto>> GetCategoryCrawlAsync(string[] categories, int perCategory = 12, CancellationToken cancellationToken = default);
}
