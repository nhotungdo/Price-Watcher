using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Services.Interfaces;
using System.Security.Claims;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/search")]
public class SearchSuggestionController : ControllerBase
{
    private readonly ISearchSuggestionService _searchSuggestionService;
    private readonly ILogger<SearchSuggestionController> _logger;

    public SearchSuggestionController(
        ISearchSuggestionService searchSuggestionService,
        ILogger<SearchSuggestionController> logger)
    {
        _searchSuggestionService = searchSuggestionService;
        _logger = logger;
    }

    /// <summary>
    /// Get search suggestions based on query
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string? q,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var suggestions = await _searchSuggestionService.GetSuggestionsAsync(
                q ?? string.Empty, 
                userId, 
                limit, 
                cancellationToken);

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", q);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get trending keywords
    /// </summary>
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var trending = await _searchSuggestionService.GetTrendingKeywordsAsync(limit, cancellationToken);
            return Ok(new { success = true, trending });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending keywords");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Record a search action
    /// </summary>
    [HttpPost("record")]
    public async Task<IActionResult> RecordSearch(
        [FromBody] RecordSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            await _searchSuggestionService.RecordSearchAsync(request.Keyword, userId, cancellationToken);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search for keyword: {Keyword}", request.Keyword);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = User?.FindFirst("uid")?.Value ?? 
                         User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}

public record RecordSearchRequest(string Keyword);
