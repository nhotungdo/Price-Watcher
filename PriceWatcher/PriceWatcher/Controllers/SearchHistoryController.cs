using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Services.Interfaces;
using System.Security.Claims;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchHistoryController : ControllerBase
{
    private readonly ISearchHistoryService _searchHistoryService;
    private readonly ILogger<SearchHistoryController> _logger;

    public SearchHistoryController(
        ISearchHistoryService searchHistoryService,
        ILogger<SearchHistoryController> logger)
    {
        _searchHistoryService = searchHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get user's recent search history
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentHistory(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Ok(new { success = false, message = "User not authenticated", history = Array.Empty<object>() });
        }

        try
        {
            var history = await _searchHistoryService.GetUserHistoryAsync(
                userId.Value,
                page: 1,
                pageSize: limit,
                cancellationToken: cancellationToken
            );

            return Ok(new
            {
                success = true,
                history = history.Select(h => new
                {
                    historyId = h.HistoryId,
                    searchType = h.SearchType,
                    keyword = h.DetectedKeyword ?? h.InputContent,
                    inputContent = h.InputContent,
                    bestPrice = h.BestPriceFound,
                    searchTime = h.SearchTime,
                    timeAgo = GetTimeAgo(h.SearchTime)
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search history for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Error loading search history" });
        }
    }

    /// <summary>
    /// Get paginated search history with filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        try
        {
            var history = await _searchHistoryService.GetUserHistoryAsync(
                userId.Value,
                page,
                pageSize,
                keyword,
                startDate,
                endDate,
                cancellationToken
            );

            return Ok(new
            {
                success = true,
                page,
                pageSize,
                history = history.Select(h => new
                {
                    historyId = h.HistoryId,
                    searchType = h.SearchType,
                    keyword = h.DetectedKeyword ?? h.InputContent,
                    inputContent = h.InputContent,
                    bestPrice = h.BestPriceFound,
                    searchTime = h.SearchTime,
                    timeAgo = GetTimeAgo(h.SearchTime)
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search history for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Error loading search history" });
        }
    }

    /// <summary>
    /// Delete a search history item
    /// </summary>
    [HttpDelete("{historyId}")]
    public async Task<IActionResult> DeleteHistory(
        int historyId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        try
        {
            var deleted = await _searchHistoryService.DeleteHistoryAsync(
                userId.Value,
                historyId,
                cancellationToken
            );

            if (!deleted)
            {
                return NotFound(new { success = false, message = "History item not found" });
            }

            return Ok(new { success = true, message = "History item deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting history {HistoryId} for user {UserId}", historyId, userId);
            return StatusCode(500, new { success = false, message = "Error deleting history item" });
        }
    }

    /// <summary>
    /// Clear all search history for the user
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearHistory(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        try
        {
            // Get all history items and delete them
            var history = await _searchHistoryService.GetUserHistoryAsync(
                userId.Value,
                page: 1,
                pageSize: 1000, // Get all
                cancellationToken: cancellationToken
            );

            foreach (var item in history)
            {
                await _searchHistoryService.DeleteHistoryAsync(userId.Value, item.HistoryId, cancellationToken);
            }

            return Ok(new { success = true, message = "All history cleared" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing history for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Error clearing history" });
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

    private string GetTimeAgo(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return "";

        var timeSpan = DateTime.UtcNow - dateTime.Value;

        if (timeSpan.TotalMinutes < 1)
            return "Vừa xong";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} phút trước";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} giờ trước";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} ngày trước";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} tuần trước";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} tháng trước";

        return $"{(int)(timeSpan.TotalDays / 365)} năm trước";
    }
}
