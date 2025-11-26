using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Services.Interfaces;
using System.Security.Claims;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(
        IFavoriteService favoriteService,
        ILogger<FavoritesController> logger)
    {
        _favoriteService = favoriteService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetFavorites([FromQuery] string? collection = null)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var favorites = await _favoriteService.GetUserFavoritesAsync(userId.Value, collection);
            return Ok(new
            {
                success = true,
                count = favorites.Count,
                favorites = favorites.Select(f => new
                {
                    id = f.Id,
                    productId = f.ProductId,
                    productName = f.Product?.ProductName,
                    productImage = f.Product?.ImageUrl,
                    currentPrice = f.Product?.CurrentPrice,
                    platform = f.Product?.Platform?.PlatformName,
                    collection = f.CollectionName,
                    notes = f.Notes,
                    targetPrice = f.TargetPrice,
                    notifyOnPriceDrop = f.NotifyOnPriceDrop,
                    createdAt = f.CreatedAt,
                    lastViewedAt = f.LastViewedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorites for user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving favorites" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        if (request.ProductId <= 0)
        {
            return BadRequest(new { message = "Invalid product ID" });
        }

        try
        {
            var favorite = await _favoriteService.AddToFavoritesAsync(
                userId.Value,
                request.ProductId,
                request.Collection,
                request.Notes);

            return Ok(new
            {
                success = true,
                message = "Product added to favorites",
                favorite = new
                {
                    id = favorite.Id,
                    productId = favorite.ProductId,
                    collection = favorite.CollectionName
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding favorite for user {UserId}", userId);
            return StatusCode(500, new { message = "Error adding to favorites" });
        }
    }

    [HttpDelete("{favoriteId}")]
    public async Task<IActionResult> RemoveFavorite(int favoriteId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var success = await _favoriteService.RemoveFromFavoritesAsync(userId.Value, favoriteId);
            if (!success)
            {
                return NotFound(new { message = "Favorite not found" });
            }

            return Ok(new { success = true, message = "Favorite removed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing favorite {FavoriteId}", favoriteId);
            return StatusCode(500, new { message = "Error removing favorite" });
        }
    }

    [HttpPut("{favoriteId}")]
    public async Task<IActionResult> UpdateFavorite(int favoriteId, [FromBody] UpdateFavoriteRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var success = await _favoriteService.UpdateFavoriteAsync(
                userId.Value,
                favoriteId,
                request.Collection,
                request.Notes,
                request.TargetPrice);

            if (!success)
            {
                return NotFound(new { message = "Favorite not found" });
            }

            return Ok(new { success = true, message = "Favorite updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating favorite {FavoriteId}", favoriteId);
            return StatusCode(500, new { message = "Error updating favorite" });
        }
    }

    [HttpGet("collections")]
    public async Task<IActionResult> GetCollections()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var collections = await _favoriteService.GetUserCollectionsAsync(userId.Value);
            return Ok(new { success = true, collections });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections for user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving collections" });
        }
    }

    [HttpGet("check/{productId}")]
    public async Task<IActionResult> CheckFavorite(int productId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Ok(new { isFavorite = false });
        }

        try
        {
            var isFavorite = await _favoriteService.IsFavoriteAsync(userId.Value, productId);
            return Ok(new { isFavorite });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking favorite status");
            return Ok(new { isFavorite = false });
        }
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetFavoriteCount()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Ok(new { count = 0 });
        }

        try
        {
            var count = await _favoriteService.GetFavoriteCountAsync(userId.Value);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite count");
            return Ok(new { count = 0 });
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = User?.FindFirst("uid")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

public class AddFavoriteRequest
{
    public int ProductId { get; set; }
    public string? Collection { get; set; }
    public string? Notes { get; set; }
}

public class UpdateFavoriteRequest
{
    public string? Collection { get; set; }
    public string? Notes { get; set; }
    public decimal? TargetPrice { get; set; }
}
