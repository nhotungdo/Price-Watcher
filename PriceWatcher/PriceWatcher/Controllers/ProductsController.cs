using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;
using System.Security.Claims;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly PriceWatcherDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        PriceWatcherDbContext context,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Track a product by URL - creates or updates product and price history
    /// </summary>
    /// <param name="request">Request containing product URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details with price history</returns>
    [HttpPost("track")]
    [ProducesResponseType(typeof(ProductTrackingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TrackProduct(
        [FromBody] TrackProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { error = "Product URL is required" });
        }

        // Get user ID if authenticated
        var userId = GetUserId();

        _logger.LogInformation("Track product request: {Url}, User: {UserId}", request.Url, userId);

        try
        {
            var result = await _productService.TrackProductByUrlAsync(
                request.Url,
                userId,
                cancellationToken
            );

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while tracking product");
            return BadRequest(new { error = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Unsupported platform");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking product: {Url}", request.Url);
            return StatusCode(500, new { error = "An error occurred while tracking the product" });
        }
    }

    /// <summary>
    /// Get product details with complete price history
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product with price history chart data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductWithHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await _productService.GetProductWithHistoryAsync(id, cancellationToken);

        if (product == null)
        {
            return NotFound(new { error = "Product not found" });
        }

        return Ok(product);
    }

    /// <summary>
    /// Check if a product URL exists in the database
    /// </summary>
    /// <param name="request">Request containing product URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product ID if exists, null otherwise</returns>
    [HttpPost("check")]
    [ProducesResponseType(typeof(ProductExistsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckProductExists(
        [FromBody] CheckProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { error = "Product URL is required" });
        }

        var productId = await _productService.FindProductByUrlAsync(request.Url, cancellationToken);

        return Ok(new ProductExistsResponse
        {
            Exists = productId.HasValue,
            ProductId = productId
        });
    }

    /// <summary>
    /// Track product and get price history (combined endpoint)
    /// </summary>
    /// <param name="request">Request containing product URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details with price history</returns>
    [HttpPost("track-and-history")]
    [ProducesResponseType(typeof(ProductTrackingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TrackAndGetHistory(
        [FromBody] TrackProductRequest request,
        CancellationToken cancellationToken = default)
    {
        // This is essentially the same as TrackProduct, but with explicit naming
        return await TrackProduct(request, cancellationToken);
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{categoryName}")]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsByCategory(
        string categoryName,
        [FromQuery] int limit = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all products and return them (category filtering can be added later)
            var products = await _context.Products
                .OrderByDescending(p => p.LastUpdated)
                .Take(limit)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName ?? "",
                    ImageUrl = p.ImageUrl,
                    CurrentPrice = p.CurrentPrice ?? 0,
                    OriginalPrice = p.OriginalPrice,
                    DiscountRate = p.DiscountRate,
                    Platform = p.Platform != null ? p.Platform.PlatformName : "Unknown",
                    OriginalUrl = p.OriginalUrl,
                    Rating = p.Rating,
                    ReviewCount = p.ReviewCount
                })
                .ToListAsync(cancellationToken);

            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for category {CategoryName}", categoryName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst("uid")?.Value ?? 
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Assign a product to a category
    /// </summary>
    [HttpPut("{productId}/category/{categoryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignCategory(
        int productId,
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            var category = await _context.Categories.FindAsync(new object[] { categoryId }, cancellationToken);
            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            product.CategoryId = categoryId;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Assigned product {ProductId} to category {CategoryId}", productId, categoryId);

            return Ok(new { message = "Category assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning category to product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to assign category" });
        }
    }

    /// <summary>
    /// Remove category from a product
    /// </summary>
    [HttpDelete("{productId}/category")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCategory(
        int productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            product.CategoryId = null;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed category from product {ProductId}", productId);

            return Ok(new { message = "Category removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing category from product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to remove category" });
        }
    }
}

public class ProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int? DiscountRate { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? OriginalUrl { get; set; }
    public double? Rating { get; set; }
    public int? ReviewCount { get; set; }
}

#region Request/Response Models

public record TrackProductRequest(string Url);

public record CheckProductRequest(string Url);

public class ProductExistsResponse
{
    public bool Exists { get; set; }
    public int? ProductId { get; set; }
}

#endregion
