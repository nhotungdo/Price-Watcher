using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class CartService : ICartService
{
    private static readonly TimeSpan AnonymousCartTtl = TimeSpan.FromDays(7);
    private const int MaxQuantity = 99;

    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILogger<CartService> _logger;

    public CartService(PriceWatcherDbContext dbContext, ILogger<CartService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(int? userId, Guid? anonymousId, CancellationToken cancellationToken = default)
    {
        await CleanupExpiredCartsAsync(cancellationToken);
        var cart = await LoadCartAsync(userId, anonymousId, createIfMissing: true, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(AddCartItemRequest request, int? userId, Guid? anonymousId, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0 || request.Quantity > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Quantity), $"Quantity must be between 1 and {MaxQuantity}");
        }

        await CleanupExpiredCartsAsync(cancellationToken);
        var cart = await LoadCartAsync(userId, anonymousId, createIfMissing: true, cancellationToken);
        
        // Find existing item by ProductUrl (for external products) or by ProductId+PlatformId (for tracked products)
        var item = cart.Items.FirstOrDefault(i => 
            (!string.IsNullOrEmpty(request.ProductUrl) && i.ProductUrl == request.ProductUrl) ||
            (request.ProductId > 0 && i.ProductId == request.ProductId && i.PlatformId == request.PlatformId));

        if (item == null)
        {
            item = new CartItem
            {
                ProductId = request.ProductId,
                ProductName = request.Name,
                PlatformId = request.PlatformId,
                PlatformName = request.PlatformName,
                Price = request.Price,
                OriginalPrice = request.OriginalPrice,
                ImageUrl = request.ImageUrl,
                ProductUrl = request.ProductUrl,
                Quantity = Math.Clamp(request.Quantity, 1, MaxQuantity),
                AddedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            cart.Items.Add(item);
        }
        else
        {
            item.Quantity = Math.Clamp(item.Quantity + request.Quantity, 1, MaxQuantity);
            item.Price = request.Price;
            item.OriginalPrice = request.OriginalPrice;
            item.PlatformName = request.PlatformName ?? item.PlatformName;
            item.ImageUrl = request.ImageUrl ?? item.ImageUrl;
            item.ProductUrl = request.ProductUrl ?? item.ProductUrl;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await UpdateCartTimestampsAsync(cart, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> UpdateQuantityAsync(int productId, int? platformId, int quantity, int? userId, Guid? anonymousId, CancellationToken cancellationToken = default)
    {
        if (quantity < 1 || quantity > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        await CleanupExpiredCartsAsync(cancellationToken);
        var cart = await LoadCartAsync(userId, anonymousId, createIfMissing: false, cancellationToken);
        if (cart == null)
        {
            return new CartDto();
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.PlatformId == platformId);
        if (item == null)
        {
            return ToDto(cart);
        }

        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await UpdateCartTimestampsAsync(cart, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> UpdateQuantityByCartItemIdAsync(int cartItemId, int quantity, int? userId, Guid? anonymousId, CancellationToken cancellationToken = default)
    {
        if (quantity < 1 || quantity > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        await CleanupExpiredCartsAsync(cancellationToken);
        var cart = await LoadCartAsync(userId, anonymousId, createIfMissing: false, cancellationToken);
        if (cart == null)
        {
            return new CartDto();
        }

        var item = cart.Items.FirstOrDefault(i => i.CartItemId == cartItemId);
        if (item == null)
        {
            return ToDto(cart);
        }

        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await UpdateCartTimestampsAsync(cart, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(int productId, int? platformId, int? userId, Guid? anonymousId, CancellationToken cancellationToken = default)
    {
        await CleanupExpiredCartsAsync(cancellationToken);
        var cart = await LoadCartAsync(userId, anonymousId, createIfMissing: false, cancellationToken);
        if (cart == null)
        {
            return new CartDto();
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.PlatformId == platformId);
        if (item != null)
        {
            cart.Items.Remove(item);
        }

        await UpdateCartTimestampsAsync(cart, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> RemoveItemByCartItemIdAsync(int cartItemId, int? userId, Guid? anonymousId, CancellationToken cancellationToken = default)
    {
        await CleanupExpiredCartsAsync(cancellationToken);
        var cart = await LoadCartAsync(userId, anonymousId, createIfMissing: false, cancellationToken);
        if (cart == null)
        {
            return new CartDto();
        }

        var item = cart.Items.FirstOrDefault(i => i.CartItemId == cartItemId);
        if (item != null)
        {
            cart.Items.Remove(item);
        }

        await UpdateCartTimestampsAsync(cart, cancellationToken);
        return ToDto(cart);
    }

    public async Task MergeAsync(int userId, Guid anonymousId, CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return;
        }

        await CleanupExpiredCartsAsync(cancellationToken);
        var userCart = await LoadCartAsync(userId, null, createIfMissing: true, cancellationToken);
        var anonymousCart = await LoadCartAsync(null, anonymousId, createIfMissing: false, cancellationToken);

        if (anonymousCart == null || anonymousCart.Items.Count == 0)
        {
            return;
        }

        foreach (var anonItem in anonymousCart.Items)
        {
            var existing = userCart.Items.FirstOrDefault(i => i.ProductId == anonItem.ProductId && i.PlatformId == anonItem.PlatformId);
            if (existing == null)
            {
                userCart.Items.Add(new CartItem
                {
                    ProductId = anonItem.ProductId,
                    ProductName = anonItem.ProductName,
                    PlatformId = anonItem.PlatformId,
                    PlatformName = anonItem.PlatformName,
                    Price = anonItem.Price,
                    OriginalPrice = anonItem.OriginalPrice,
                    ImageUrl = anonItem.ImageUrl,
                    ProductUrl = anonItem.ProductUrl,
                    Quantity = Math.Clamp(anonItem.Quantity, 1, MaxQuantity),
                    AddedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Quantity = Math.Clamp(existing.Quantity + anonItem.Quantity, 1, MaxQuantity);
                existing.Price = anonItem.Price;
                existing.OriginalPrice = anonItem.OriginalPrice ?? existing.OriginalPrice;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        _dbContext.Carts.Remove(anonymousCart);
        await UpdateCartTimestampsAsync(userCart, cancellationToken);
    }

    public async Task DeactivateAnonymousCartAsync(Guid anonymousId, CancellationToken cancellationToken = default)
    {
        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.AnonymousId == anonymousId, cancellationToken);
        if (cart == null)
        {
            return;
        }

        _dbContext.Carts.Remove(cart);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Cart?> LoadCartAsync(int? userId, Guid? anonymousId, bool createIfMissing, CancellationToken cancellationToken)
    {
        Cart? cart = null;
        if (userId.HasValue)
        {
            cart = await _dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken);
            if (cart == null && createIfMissing)
            {
                cart = new Cart
                {
                    UserId = userId.Value,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Carts.Add(cart);
                await _dbContext.SaveChangesAsync(cancellationToken);
                cart = await _dbContext.Carts.Include(c => c.Items).FirstAsync(c => c.CartId == cart.CartId, cancellationToken);
            }
        }
        else if (anonymousId.HasValue)
        {
            cart = await _dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.AnonymousId == anonymousId.Value, cancellationToken);
            if (cart == null && createIfMissing)
            {
                cart = new Cart
                {
                    AnonymousId = anonymousId.Value,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(AnonymousCartTtl)
                };
                _dbContext.Carts.Add(cart);
                await _dbContext.SaveChangesAsync(cancellationToken);
                cart = await _dbContext.Carts.Include(c => c.Items).FirstAsync(c => c.CartId == cart.CartId, cancellationToken);
            }
        }

        return cart;
    }

    private async Task UpdateCartTimestampsAsync(Cart cart, CancellationToken cancellationToken)
    {
        cart.UpdatedAt = DateTime.UtcNow;
        if (cart.AnonymousId.HasValue)
        {
            cart.ExpiresAt = DateTime.UtcNow.Add(AnonymousCartTtl);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CleanupExpiredCartsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await _dbContext.Carts
            .Where(c => c.AnonymousId != null && c.ExpiresAt != null && c.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        _dbContext.Carts.RemoveRange(expired);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Removed {Count} expired anonymous carts", expired.Count);
    }

    private static CartDto ToDto(Cart? cart)
    {
        if (cart == null)
        {
            return new CartDto();
        }

        var items = cart.Items
            .OrderByDescending(i => i.UpdatedAt)
            .Select(i => new CartItemDto
            {
                CartItemId = i.CartItemId,
                ProductId = i.ProductId,
                PlatformId = i.PlatformId,
                PlatformName = i.PlatformName,
                Name = i.ProductName,
                Price = i.Price,
                OriginalPrice = i.OriginalPrice,
                ImageUrl = i.ImageUrl,
                ProductUrl = i.ProductUrl,
                Quantity = i.Quantity
            })
            .ToList();

        var subtotal = items.Sum(i => i.Price * i.Quantity);
        var discount = items.Sum(i =>
        {
            if (i.OriginalPrice.HasValue && i.OriginalPrice.Value > i.Price)
            {
                return (i.OriginalPrice.Value - i.Price) * i.Quantity;
            }
            return 0m;
        });

        return new CartDto
        {
            Items = items,
            Summary = new CartSummaryDto
            {
                Subtotal = subtotal,
                Discount = discount,
                Total = subtotal,
                ItemCount = items.Sum(i => i.Quantity)
            },
            IsAuthenticatedCart = cart.UserId.HasValue
        };
    }
}

