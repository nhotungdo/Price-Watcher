using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class FavoriteService : IFavoriteService
{
    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILogger<FavoriteService> _logger;

    public FavoriteService(
        PriceWatcherDbContext dbContext,
        ILogger<FavoriteService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<Favorite>> GetUserFavoritesAsync(int userId, string? collection = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Favorite>()
            .Include(f => f.Product)
                .ThenInclude(p => p!.Platform)
            .Include(f => f.Product)
                .ThenInclude(p => p!.Category)
            .Where(f => f.UserId == userId);

        if (!string.IsNullOrWhiteSpace(collection))
        {
            query = query.Where(f => f.CollectionName == collection);
        }

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Favorite?> AddToFavoritesAsync(int userId, int productId, string? collection = null, string? notes = null, CancellationToken cancellationToken = default)
    {
        // Check if already favorited
        var existing = await _dbContext.Set<Favorite>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId, cancellationToken);

        if (existing != null)
        {
            // Update existing
            existing.CollectionName = collection;
            existing.Notes = notes;
            existing.LastViewedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        // Create new
        var favorite = new Favorite
        {
            UserId = userId,
            ProductId = productId,
            CollectionName = collection,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            LastViewedAt = DateTime.UtcNow
        };

        _dbContext.Set<Favorite>().Add(favorite);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} added product {ProductId} to favorites", userId, productId);
        return favorite;
    }

    public async Task<bool> RemoveFromFavoritesAsync(int userId, int favoriteId, CancellationToken cancellationToken = default)
    {
        var favorite = await _dbContext.Set<Favorite>()
            .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == userId, cancellationToken);

        if (favorite == null)
        {
            return false;
        }

        _dbContext.Set<Favorite>().Remove(favorite);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} removed favorite {FavoriteId}", userId, favoriteId);
        return true;
    }

    public async Task<bool> UpdateFavoriteAsync(int userId, int favoriteId, string? collection, string? notes, decimal? targetPrice, CancellationToken cancellationToken = default)
    {
        var favorite = await _dbContext.Set<Favorite>()
            .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == userId, cancellationToken);

        if (favorite == null)
        {
            return false;
        }

        favorite.CollectionName = collection;
        favorite.Notes = notes;
        favorite.TargetPrice = targetPrice;
        favorite.LastViewedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<string>> GetUserCollectionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Favorite>()
            .Where(f => f.UserId == userId && f.CollectionName != null)
            .Select(f => f.CollectionName!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsFavoriteAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Favorite>()
            .AnyAsync(f => f.UserId == userId && f.ProductId == productId, cancellationToken);
    }

    public async Task<int> GetFavoriteCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Favorite>()
            .CountAsync(f => f.UserId == userId, cancellationToken);
    }
}
