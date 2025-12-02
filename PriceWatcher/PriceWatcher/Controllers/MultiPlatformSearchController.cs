using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MultiPlatformSearchController : ControllerBase
{
    private readonly IMultiPlatformSearchService _searchService;
    private readonly ILogger<MultiPlatformSearchController> _logger;

    public MultiPlatformSearchController(
        IMultiPlatformSearchService searchService,
        ILogger<MultiPlatformSearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Search for products across Shopee, Lazada, and Tiki
    /// </summary>
    /// <param name="request">Search request with keyword and filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Products from all platforms</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(MultiPlatformSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromBody] MultiPlatformSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            return BadRequest(new { error = "Keyword is required" });
        }

        _logger.LogInformation("Multi-platform search request: {Keyword}", request.Keyword);

        try
        {
            var result = await _searchService.SearchAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during multi-platform search: {Keyword}", request.Keyword);
            return StatusCode(500, new { error = "An error occurred during search" });
        }
    }

    /// <summary>
    /// Search with query parameters (GET method for simple searches)
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(MultiPlatformSearchResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchGet(
        [FromQuery] string keyword,
        [FromQuery] string? platforms,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] double? minRating = null,
        [FromQuery] bool? freeShipping = null,
        [FromQuery] bool? officialStore = null,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { error = "Keyword is required" });
        }

        var request = new MultiPlatformSearchRequest
        {
            Keyword = keyword,
            Platforms = platforms?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Limit = limit,
            Offset = offset,
            SortBy = sortBy,
            Filters = new MultiPlatformSearchFilters
            {
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating,
                FreeShipping = freeShipping,
                OfficialStore = officialStore
            }
        };

        try
        {
            var result = await _searchService.SearchAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during multi-platform search: {Keyword}", keyword);
            return StatusCode(500, new { error = "An error occurred during search" });
        }
    }

    /// <summary>
    /// Compare prices for a product across platforms
    /// </summary>
    [HttpGet("compare")]
    [ProducesResponseType(typeof(List<PriceComparisonDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ComparePrices(
        [FromQuery] string keyword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { error = "Keyword is required" });
        }

        try
        {
            var result = await _searchService.ComparePricesAsync(keyword, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing prices: {Keyword}", keyword);
            return StatusCode(500, new { error = "An error occurred during price comparison" });
        }
    }

    /// <summary>
    /// Get available platforms
    /// </summary>
    [HttpGet("platforms")]
    [ProducesResponseType(typeof(List<PlatformInfo>), StatusCodes.Status200OK)]
    public IActionResult GetPlatforms()
    {
        var platforms = new List<PlatformInfo>
        {
            new() { Name = "Shopee", Code = "shopee", Logo = "/images/platforms/shopee-logo.png", IsActive = true },
            new() { Name = "Lazada", Code = "lazada", Logo = "/images/platforms/lazada-logo.png", IsActive = true },
            new() { Name = "Tiki", Code = "tiki", Logo = "/images/platforms/tiki-logo.png", IsActive = true }
        };

        return Ok(platforms);
    }
}

public class PlatformInfo
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
