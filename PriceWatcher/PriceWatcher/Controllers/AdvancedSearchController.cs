using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/search")]
public class AdvancedSearchController : ControllerBase
{
    private readonly IAdvancedSearchService _advancedSearchService;
    private readonly ILogger<AdvancedSearchController> _logger;

    public AdvancedSearchController(
        IAdvancedSearchService advancedSearchService,
        ILogger<AdvancedSearchController> logger)
    {
        _advancedSearchService = advancedSearchService;
        _logger = logger;
    }

    [HttpPost("advanced")]
    public async Task<IActionResult> AdvancedSearch([FromBody] SearchFilters filters, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _advancedSearchService.SearchWithFiltersAsync(filters, cancellationToken);
            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced search with filters: {@Filters}", filters);
            return StatusCode(500, new { success = false, message = "Error performing search" });
        }
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string q, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new { suggestions = Array.Empty<string>() });
        }

        try
        {
            var suggestions = await _advancedSearchService.GetSearchSuggestionsAsync(q, limit, cancellationToken);
            return Ok(new { suggestions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", q);
            return Ok(new { suggestions = Array.Empty<string>() });
        }
    }

    [HttpGet("filters/categories")]
    public async Task<IActionResult> GetCategoryCounts([FromQuery] string? q = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var counts = await _advancedSearchService.GetCategoryCountsAsync(q, cancellationToken);
            return Ok(new { success = true, categories = counts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category counts");
            return StatusCode(500, new { success = false, message = "Error retrieving categories" });
        }
    }

    [HttpGet("filters/platforms")]
    public async Task<IActionResult> GetPlatformCounts([FromQuery] string? q = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var counts = await _advancedSearchService.GetPlatformCountsAsync(q, cancellationToken);
            return Ok(new { success = true, platforms = counts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform counts");
            return StatusCode(500, new { success = false, message = "Error retrieving platforms" });
        }
    }

    [HttpGet("filters/price-range")]
    public async Task<IActionResult> GetPriceRange([FromQuery] string? q = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var (min, max) = await _advancedSearchService.GetPriceRangeAsync(q, cancellationToken);
            return Ok(new { success = true, minPrice = min, maxPrice = max });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price range");
            return StatusCode(500, new { success = false, message = "Error retrieving price range" });
        }
    }
}
