using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int? userId, Guid? anonymousId, CancellationToken cancellationToken = default);
    Task<CartDto> AddItemAsync(AddCartItemRequest request, int? userId, Guid? anonymousId, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateQuantityAsync(int productId, int? platformId, int quantity, int? userId, Guid? anonymousId, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(int productId, int? platformId, int? userId, Guid? anonymousId, CancellationToken cancellationToken = default);
    Task MergeAsync(int userId, Guid anonymousId, CancellationToken cancellationToken = default);
    Task DeactivateAnonymousCartAsync(Guid anonymousId, CancellationToken cancellationToken = default);
}

