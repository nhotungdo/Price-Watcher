using PriceWatcher.Models;

namespace PriceWatcher.Services.Interfaces;

public interface IStoreListingService
{
    Task<List<StoreListing>> GetProductListingsAsync(int productId, CancellationToken cancellationToken = default);
    Task<StoreListing?> GetCheapestListingAsync(int productId, CancellationToken cancellationToken = default);
    Task<List<StoreListing>> GetListingsSortedByPriceAsync(int productId, bool ascending = true, CancellationToken cancellationToken = default);
    Task<List<StoreListing>> GetListingsSortedByRatingAsync(int productId, CancellationToken cancellationToken = default);
    Task<List<StoreListing>> GetVerifiedListingsAsync(int productId, CancellationToken cancellationToken = default);
    Task<StoreListing?> CreateOrUpdateListingAsync(StoreListing listing, CancellationToken cancellationToken = default);
}
