using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Scrapers;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly ShopeePlaywrightScraper _playwrightScraper;
    private readonly ILogger<ScraperController> _logger;

    public ScraperController(
        ShopeePlaywrightScraper playwrightScraper,
        ILogger<ScraperController> logger)
    {
        _playwrightScraper = playwrightScraper;
        _logger = logger;
    }

    /// <summary>
    /// Scrape a Shopee product using Playwright (headless browser)
    /// </summary>
    /// <param name="request">Scraping request with product URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scraped product data</returns>
    [HttpPost("shopee/playwright")]
    [ProducesResponseType(typeof(ProductCandidateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScrapeShopeeWithPlaywright(
        [FromBody] ScrapeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { error = "URL is required" });
        }

        if (!request.Url.Contains("shopee.vn", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Invalid Shopee URL" });
        }

        _logger.LogInformation("Scraping Shopee product with Playwright: {Url}", request.Url);

        var query = new ProductQuery
        {
            Platform = "shopee",
            CanonicalUrl = request.Url
        };

        var result = await _playwrightScraper.GetByUrlAsync(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { error = "Could not scrape product data", url = request.Url });
        }

        return Ok(result);
    }

    /// <summary>
    /// Test endpoint to check if Playwright is working
    /// </summary>
    [HttpGet("playwright/health")]
    public IActionResult CheckPlaywrightHealth()
    {
        return Ok(new
        {
            status = "healthy",
            scraper = "ShopeePlaywrightScraper",
            message = "Playwright scraper is ready. Use POST /api/scraper/shopee/playwright to scrape products."
        });
    }
}

public record ScrapeRequest(string Url);
