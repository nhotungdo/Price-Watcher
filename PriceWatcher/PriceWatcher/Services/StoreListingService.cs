using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class StoreListingService : IStoreListingService
{
    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILogger<StoreListingService> _logger;

    public StoreListingService(
        PriceWatcherDbContext dbContext,
        ILogger<StoreListingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<StoreListing>> GetProductListingsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StoreListing>()
            .Include(sl => sl.Platform)
            .Where(sl => sl.ProductId == productId)
            .OrderBy(sl => sl.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<StoreListing?> GetCheapestListingAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StoreListing>()
            .Include(sl => sl.Platform)
            .Where(sl => sl.ProductId == productId && sl.Stock > 0)
            .OrderBy(sl => sl.Price + (sl.ShippingCost ?? 0))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<StoreListing>> GetListingsSortedByPriceAsync(int productId, bool ascending = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<StoreListing>()
            .Include(sl => sl.Platform)
            .Where(sl => sl.ProductId == productId);

        return ascending
            ? await query.OrderBy(sl => sl.Price).ToListAsync(cancellationToken)
            : await query.OrderByDescending(sl => sl.Price).ToListAsync(cancellationToken);
    }

    public async Task<List<StoreListing>> GetListingsSortedByRatingAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StoreListing>()
            .Include(sl => sl.Platform)
            .Where(sl => sl.ProductId == productId)
            .OrderByDescending(sl => sl.StoreRating ?? 0)
            .ThenBy(sl => sl.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StoreListing>> GetVerifiedListingsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<StoreListing>()
            .Include(sl => sl.Platform)
            .Where(sl => sl.ProductId == productId && sl.IsVerified)
            .OrderBy(sl => sl.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<StoreListing?> CreateOrUpdateListingAsync(StoreListing listing, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Set<StoreListing>()
            .FirstOrDefaultAsync(sl => 
                sl.ProductId == listing.ProductId && 
                sl.PlatformId == listing.PlatformId && 
                sl.StoreName == listing.StoreName, 
                cancellationToken);

        if (existing != null)
        {
            // Update existing
            existing.Price = listing.Price;
            existing.OriginalPrice = listing.OriginalPrice;
            existing.ShippingCost = listing.ShippingCost;
            existing.Stock = listing.Stock;
            existing.DeliveryDays = listing.DeliveryDays;
            existing.StoreRating = listing.StoreRating;
            existing.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            // Create new
            _dbContext.Set<StoreListing>().Add(listing);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing ?? listing;
    }
}
