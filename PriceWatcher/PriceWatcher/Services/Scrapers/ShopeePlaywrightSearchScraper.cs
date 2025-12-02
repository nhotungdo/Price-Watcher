using Microsoft.Playwright;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;
using System.Text.RegularExpressions;

namespace PriceWatcher.Services.Scrapers;

/// <summary>
/// Shopee scraper using Playwright for search functionality
/// This bypasses anti-bot detection by using a real browser
/// </summary>
public class ShopeePlaywrightSearchScraper : IProductScraper, IAsyncDisposable
{
    private readonly ILogger<ShopeePlaywrightSearchScraper> _logger;
    private readonly IMetricsService _metrics;
    private readonly SemaphoreSlim _rateLimiter;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public string Platform => "shopee";

    public ShopeePlaywrightSearchScraper(
        ILogger<ShopeePlaywrightSearchScraper> logger,
        IMetricsService metrics)
    {
        _logger = logger;
        _metrics = metrics;
        _rateLimiter = new SemaphoreSlim(2, 2); // Max 2 concurrent searches
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            _logger.LogInformation("Initializing Playwright for Shopee search scraper...");
            
            _playwright = await Playwright.CreateAsync();
            
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-dev-shm-usage",
                    "--no-sandbox"
                }
            });

            _isInitialized = true;
            _logger.LogInformation("Playwright initialized successfully for search");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Playwright");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<IEnumerable<ProductCandidateDto>> SearchByQueryAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            await EnsureInitializedAsync();

            if (_browser == null)
            {
                _logger.LogError("Browser not initialized");
                return Array.Empty<ProductCandidateDto>();
            }

            var keyword = string.IsNullOrWhiteSpace(query.TitleHint) ? query.ProductId : query.TitleHint!;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                _logger.LogWarning("Empty keyword for Shopee search");
                return Array.Empty<ProductCandidateDto>();
            }

            _logger.LogInformation("Searching Shopee with Playwright for: {Keyword}", keyword);

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "vi-VN"
            });

            await context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => false });
                window.chrome = { runtime: {} };
            ");

            var page = await context.NewPageAsync();
            var products = new List<ProductCandidateDto>();

            try
            {
                var searchUrl = $"https://shopee.vn/search?keyword={Uri.EscapeDataString(keyword)}";
                await page.GotoAsync(searchUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });

                // Wait for products to load
                await Task.Delay(2000, cancellationToken);

                // Extract product data from the page
                var productElements = await page.QuerySelectorAllAsync("div[data-sqe='item']");
                
                _logger.LogInformation("Found {Count} product elements on page", productElements.Count);

                foreach (var element in productElements.Take(20))
                {
                    try
                    {
                        var product = await ExtractProductFromElement(element);
                        if (product != null)
                        {
                            products.Add(product);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error extracting product from element");
                    }
                }

                _logger.LogInformation("Successfully scraped {Count} products from Shopee", products.Count);
                _metrics.RecordScraperCall(Platform, success: true, elapsedMs: sw.ElapsedMilliseconds);
            }
            finally
            {
                await page.CloseAsync();
                await context.CloseAsync();
            }

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Shopee with Playwright");
            _metrics.RecordScraperCall(Platform, success: false, elapsedMs: sw.ElapsedMilliseconds);
            return Array.Empty<ProductCandidateDto>();
        }
        finally
        {
            _rateLimiter.Release();
            sw.Stop();
        }
    }

    private async Task<ProductCandidateDto?> ExtractProductFromElement(IElementHandle element)
    {
        try
        {
            // Extract product link
            var linkElement = await element.QuerySelectorAsync("a");
            var href = linkElement != null ? await linkElement.GetAttributeAsync("href") : null;
            if (string.IsNullOrWhiteSpace(href))
                return null;

            var productUrl = href.StartsWith("http") ? href : $"https://shopee.vn{href}";

            // Extract title
            var titleElement = await element.QuerySelectorAsync("div[class*='title'], span[class*='title']");
            var title = titleElement != null ? await titleElement.TextContentAsync() : "";
            title = title?.Trim() ?? "";

            // Extract price
            var priceElement = await element.QuerySelectorAsync("span[class*='price'], div[class*='price']");
            var priceText = priceElement != null ? await priceElement.TextContentAsync() : "";
            var price = ParsePrice(priceText ?? "");

            // Extract image
            var imgElement = await element.QuerySelectorAsync("img");
            var imageUrl = imgElement != null ? await imgElement.GetAttributeAsync("src") : "";

            // Extract sold count
            var soldElement = await element.QuerySelectorAsync("div[class*='sold']");
            var soldText = soldElement != null ? await soldElement.TextContentAsync() : "";
            var soldCount = ParseSoldCount(soldText ?? "");

            if (string.IsNullOrWhiteSpace(title) || price == 0)
                return null;

            return new ProductCandidateDto
            {
                Platform = Platform,
                Title = title,
                Price = price,
                ProductUrl = productUrl,
                ThumbnailUrl = imageUrl ?? "",
                SoldCount = soldCount,
                ShippingCost = 0,
                ShopName = "Shopee",
                ShopRating = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting product data from element");
            return null;
        }
    }

    private decimal ParsePrice(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText))
            return 0;

        // Remove currency symbols and normalize
        var cleaned = Regex.Replace(priceText, @"[₫đ,.\s]", "");
        cleaned = Regex.Replace(cleaned, @"[^\d]", "");

        if (decimal.TryParse(cleaned, out var price))
        {
            // Normalize price (Shopee prices are usually in VND)
            if (price < 1000) price *= 1000; // Handle k notation
            return price;
        }

        return 0;
    }

    private int? ParseSoldCount(string soldText)
    {
        if (string.IsNullOrWhiteSpace(soldText))
            return null;

        var match = Regex.Match(soldText, @"(\d+(?:[.,]\d+)?)\s*([kK]|tr)?");
        if (match.Success && int.TryParse(match.Groups[1].Value.Replace(",", "").Replace(".", ""), out var count))
        {
            var multiplier = match.Groups[2].Value.ToLower();
            if (multiplier == "k") count *= 1000;
            else if (multiplier == "tr") count *= 1000000;
            return count;
        }

        return null;
    }

    public async Task<ProductCandidateDto?> GetByUrlAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default)
    {
        // Delegate to the main Playwright scraper for URL-based scraping
        _logger.LogInformation("GetByUrlAsync not implemented in search scraper, use ShopeePlaywrightScraper instead");
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _rateLimiter?.Dispose();
        _initLock?.Dispose();
    }
}
