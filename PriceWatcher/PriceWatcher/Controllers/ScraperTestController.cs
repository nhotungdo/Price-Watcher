using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperTestController : ControllerBase
{
    private readonly IEnumerable<IProductScraper> _scrapers;
    private readonly ILogger<ScraperTestController> _logger;

    public ScraperTestController(
        IEnumerable<IProductScraper> scrapers,
        ILogger<ScraperTestController> logger)
    {
        _scrapers = scrapers;
        _logger = logger;
    }

    /// <summary>
    /// Test all scrapers with a keyword search
    /// </summary>
    [HttpGet("test-search")]
    public async Task<IActionResult> TestSearch(
        [FromQuery] string keyword = "iPhone",
        [FromQuery] string? platform = null,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, object>();
        
        var scrapersToTest = string.IsNullOrWhiteSpace(platform)
            ? _scrapers
            : _scrapers.Where(s => s.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));

        foreach (var scraper in scrapersToTest)
        {
            try
            {
                _logger.LogInformation("Testing {Platform} scraper with keyword: {Keyword}", 
                    scraper.Platform, keyword);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                
                var query = new ProductQuery
                {
                    Platform = scraper.Platform,
                    TitleHint = keyword,
                    Metadata = new Dictionary<string, string>
                    {
                        { "limit", "5" }
                    }
                };

                var products = await scraper.SearchByQueryAsync(query, cancellationToken);
                sw.Stop();

                results[scraper.Platform] = new
                {
                    success = true,
                    count = products.Count(),
                    elapsedMs = sw.ElapsedMilliseconds,
                    products = products.Take(3).Select(p => new
                    {
                        p.Title,
                        p.Price,
                        p.ProductUrl,
                        p.ThumbnailUrl
                    })
                };

                _logger.LogInformation("{Platform} returned {Count} products in {Ms}ms",
                    scraper.Platform, products.Count(), sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing {Platform} scraper", scraper.Platform);
                results[scraper.Platform] = new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                };
            }
        }

        return Ok(new
        {
            keyword,
            timestamp = DateTime.UtcNow,
            results
        });
    }

    /// <summary>
    /// Get scraper status and configuration
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var scrapers = _scrapers.Select(s => new
        {
            platform = s.Platform,
            type = s.GetType().Name,
            available = true
        }).ToList();

        return Ok(new
        {
            totalScrapers = scrapers.Count,
            scrapers,
            timestamp = DateTime.UtcNow
        });
    }
}
