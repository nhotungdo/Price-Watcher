using PriceWatcher.Models;

namespace PriceWatcher.Services.Interfaces;

public interface IFavoriteService
{
    Task<List<Favorite>> GetUserFavoritesAsync(int userId, string? collection = null, CancellationToken cancellationToken = default);
    Task<Favorite?> AddToFavoritesAsync(int userId, int productId, string? collection = null, string? notes = null, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromFavoritesAsync(int userId, int favoriteId, CancellationToken cancellationToken = default);
    Task<bool> UpdateFavoriteAsync(int userId, int favoriteId, string? collection, string? notes, decimal? targetPrice, CancellationToken cancellationToken = default);
    Task<List<string>> GetUserCollectionsAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> IsFavoriteAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<int> GetFavoriteCountAsync(int userId, CancellationToken cancellationToken = default);
}
