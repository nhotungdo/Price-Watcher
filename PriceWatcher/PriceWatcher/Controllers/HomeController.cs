using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Services.Interfaces;
using PriceWatcher.Dtos;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/home")]
public class HomeController : ControllerBase
{
    private readonly ISuggestedProductsService _suggested;
    private readonly ISearchHistoryService _history;
    private readonly IRecommendationService _recommendation;

    public HomeController(ISuggestedProductsService suggested, ISearchHistoryService history, IRecommendationService recommendation)
    {
        _suggested = suggested;
        _history = history;
        _recommendation = recommendation;
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] int limit = 12, CancellationToken cancellationToken = default)
    {
        var products = await _suggested.GetSuggestedProductsAsync(limit, cancellationToken);
        return Ok(new { success = true, products });
    }

    [HttpGet("suggestions/{platform}")]
    public async Task<IActionResult> GetSuggestionsByPlatform(string platform, [FromQuery] int limit = 12, CancellationToken cancellationToken = default)
    {
        var products = await _suggested.GetSuggestedProductsByPlatformAsync(platform, limit, cancellationToken);
        return Ok(new { success = true, products });
    }

    [HttpGet("suggestions/personal")]
    public async Task<IActionResult> GetPersonalSuggestions([FromQuery] int limit = 12, CancellationToken cancellationToken = default)
    {
        var userId = ResolveUserId();
        if (userId == null)
        {
            var fallback = await _suggested.GetSuggestedProductsAsync(limit, cancellationToken);
            return Ok(new { success = true, products = fallback, source = "fallback" });
        }

        var histories = await _history.GetUserHistoryAsync(userId.Value, page: 1, pageSize: 10, cancellationToken: cancellationToken);
        var tokens = histories
            .Select(h => string.IsNullOrWhiteSpace(h.DetectedKeyword) ? h.InputContent : h.DetectedKeyword)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .Distinct()
            .Take(6)
            .ToArray();

        var collected = new List<ProductCandidateDto>();
        foreach (var t in tokens)
        {
            ProductQuery q;
            if (Uri.TryCreate(t, UriKind.Absolute, out var u))
            {
                var platform = u.Host.Contains("shopee", StringComparison.OrdinalIgnoreCase) ? "shopee"
                    : u.Host.Contains("lazada", StringComparison.OrdinalIgnoreCase) ? "lazada"
                    : u.Host.Contains("tiki", StringComparison.OrdinalIgnoreCase) ? "tiki" : null;
                q = new ProductQuery { CanonicalUrl = t, Platform = platform };
            }
            else
            {
                q = new ProductQuery { TitleHint = t };
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMilliseconds(500));
                var res = await _recommendation.RecommendAsync(q, top: 8, cts.Token);
                collected.AddRange(res);
            }
            catch { }
        }

        var distinct = collected
            .GroupBy(c => string.IsNullOrWhiteSpace(c.ProductUrl) ? ($"{c.Platform}:{c.Title}") : c.ProductUrl)
            .Select(g => g.OrderByDescending(x => x.MatchScore).First())
            .Take(limit)
            .Select(c => new SuggestedProductDto
            {
                ProductId = c.ProductId,
                ProductName = c.Title,
                ImageUrl = c.ThumbnailUrl,
                Price = c.TotalCost,
                OriginalPrice = c.OriginalPrice,
                DiscountRate = c.DiscountPercent.HasValue ? (int?)Math.Round(c.DiscountPercent.Value) : null,
                ProductUrl = c.ProductUrl,
                Platform = c.Platform,
                PlatformId = 0,
                PlatformLogo = string.Empty,
                PlatformColor = string.Empty,
                Rating = null,
                ReviewCount = null,
                ShopName = c.ShopName,
                IsFreeShip = c.IsFreeShip ?? false,
                LastUpdated = DateTime.Now
            })
            .ToList();

        return Ok(new { success = true, products = distinct, source = "personal" });
    }

    [HttpGet("suggestions/test")]
    public async Task<IActionResult> TestCategoryCrawl([FromQuery] int perCategory = 12, [FromQuery] bool save = false, CancellationToken cancellationToken = default)
    {
        var categories = new[] { "điện thoại", "laptop", "máy ảnh", "âm thanh", "thời trang" };
        var items = await _suggested.GetCategoryCrawlAsync(categories, perCategory, cancellationToken);
        var grouped = items.GroupBy(i => i.Platform).Select(g => new { platform = g.Key, count = g.Count() }).ToList();
        var stats = new
        {
            total = items.Count,
            byPlatform = grouped,
            byCategory = categories.Select(c => new { category = c, count = items.Count(i => (i.ProductName ?? string.Empty).Contains(c, StringComparison.OrdinalIgnoreCase)) }).ToList()
        };
        if (save)
        {
            try
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "PriceWatcher", "PriceWatcher", "wwwroot", "data");
                Directory.CreateDirectory(folder);
                var file = Path.Combine(folder, $"suggestions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
                await System.IO.File.WriteAllTextAsync(file, System.Text.Json.JsonSerializer.Serialize(new { stats, items }), cancellationToken);
            }
            catch { }
        }
        return Ok(new { success = true, stats, products = items.Take(100).ToList() });
    }

    private int? ResolveUserId()
    {
        var claim = User?.FindFirst("uid")?.Value ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out var id)) return id;
        return null;
    }
}
