using Microsoft.Playwright;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;
using System.Text.RegularExpressions;

namespace PriceWatcher.Services.Scrapers;

/// <summary>
/// Production-ready Shopee scraper using Microsoft Playwright for headless browser automation.
/// Handles anti-bot detection, timeouts, and dynamic content loading.
/// </summary>
public class ShopeePlaywrightScraper : IProductScraper, IAsyncDisposable
{
    private readonly ILogger<ShopeePlaywrightScraper> _logger;
    private readonly IMetricsService _metrics;
    private readonly SemaphoreSlim _rateLimiter;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    // Selectors for Shopee elements
    private const string PRICE_SELECTOR = "div[class*='product-price'], div[class*='pqTWkA'], div.flex.items-center";
    private const string TITLE_SELECTOR = "h1, div[class*='product-title'], span[class*='WBVL_7']";
    private const string IMAGE_SELECTOR = "img[class*='product-image'], div._2JKJFc img, img[class*='_2ZYQg']";
    private const string SHOP_NAME_SELECTOR = "div[class*='shop-name'], a[class*='_3oc7J'], div._3oc7J";
    
    public string Platform => "shopee";

    public ShopeePlaywrightScraper(
        ILogger<ShopeePlaywrightScraper> logger,
        IMetricsService metrics)
    {
        _logger = logger;
        _metrics = metrics;
        _rateLimiter = new SemaphoreSlim(3, 3); // Max 3 concurrent requests
    }

    /// <summary>
    /// Initialize Playwright and browser instance (lazy initialization)
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            _logger.LogInformation("Initializing Playwright for Shopee scraper...");
            
            _playwright = await Playwright.CreateAsync();
            
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-web-security",
                    "--disable-features=IsolateOrigins,site-per-process"
                }
            });

            _isInitialized = true;
            _logger.LogInformation("Playwright initialized successfully");
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
        // For search, we'll use the existing HTTP-based approach from ShopeeScraperStub
        // as it's more efficient for bulk searches
        _logger.LogInformation("Search by query not implemented in Playwright scraper, use ShopeeScraperStub instead");
        return Array.Empty<ProductCandidateDto>();
    }

    public async Task<ProductCandidateDto?> GetByUrlAsync(
        ProductQuery query, 
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(query.Platform, Platform, StringComparison.OrdinalIgnoreCase))
            return null;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            await EnsureInitializedAsync();

            if (_browser == null)
            {
                _logger.LogError("Browser not initialized");
                return null;
            }

            var url = query.CanonicalUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("No URL provided in query");
                return null;
            }

            _logger.LogInformation("Scraping Shopee product: {Url}", url);

            // Create a new browser context with anti-detection measures
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "vi-VN",
                TimezoneId = "Asia/Ho_Chi_Minh",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7",
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
                    ["Referer"] = "https://shopee.vn/"
                }
            });

            // Add script to mask automation
            await context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => false });
                Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                Object.defineProperty(navigator, 'languages', { get: () => ['vi-VN', 'vi', 'en-US', 'en'] });
                window.chrome = { runtime: {} };
            ");

            var page = await context.NewPageAsync();

            try
            {
                // Navigate with timeout and wait for network idle
                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000 // 30 seconds timeout
                });

                if (response == null || !response.Ok)
                {
                    _logger.LogWarning("Failed to load page: {Url}, Status: {Status}", 
                        url, response?.Status ?? 0);
                    return null;
                }

                // Wait for critical elements to load
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                
                // Additional wait for dynamic content
                await Task.Delay(2000, cancellationToken);

                // Extract product data
                var product = await ExtractProductDataAsync(page, url, cancellationToken);

                if (product != null)
                {
                    _logger.LogInformation("Successfully scraped product: {Title}", product.Title);
                    _metrics.RecordScraperCall(Platform, success: true, elapsedMs: sw.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning("Failed to extract product data from: {Url}", url);
                    _metrics.RecordScraperCall(Platform, success: false, elapsedMs: sw.ElapsedMilliseconds);
                }

                return product;
            }
            finally
            {
                await page.CloseAsync();
                await context.CloseAsync();
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while scraping Shopee URL: {Url}", query.CanonicalUrl);
            _metrics.RecordScraperCall(Platform, success: false, elapsedMs: sw.ElapsedMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Shopee URL: {Url}", query.CanonicalUrl);
            _metrics.RecordScraperCall(Platform, success: false, elapsedMs: sw.ElapsedMilliseconds);
            return null;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private async Task<ProductCandidateDto?> ExtractProductDataAsync(
        IPage page, 
        string url, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract product name
            var title = await ExtractTextAsync(page, TITLE_SELECTOR);
            if (string.IsNullOrWhiteSpace(title))
            {
                _logger.LogWarning("Could not find product title");
                return null;
            }

            // Extract price - try multiple strategies
            var price = await ExtractPriceAsync(page);
            if (price == 0)
            {
                _logger.LogWarning("Could not find product price");
                return null;
            }

            // Extract image URL
            var imageUrl = await ExtractImageUrlAsync(page);

            // Extract shop name
            var shopName = await ExtractTextAsync(page, SHOP_NAME_SELECTOR) ?? "Unknown Shop";

            // Extract additional data from page content
            var pageContent = await page.ContentAsync();
            
            // Try to extract original price and discount
            var (originalPrice, discountPercent) = ExtractPriceDetails(pageContent, price);

            // Extract sold count
            var soldCount = ExtractSoldCount(pageContent);

            // Extract rating
            var rating = ExtractRating(pageContent);

            // Check stock status
            var isOutOfStock = await CheckOutOfStockAsync(page);

            return new ProductCandidateDto
            {
                Platform = Platform,
                Title = title,
                Price = price,
                OriginalPrice = originalPrice,
                DiscountPercent = discountPercent,
                ShippingCost = 0, // Will be calculated separately
                ShopName = shopName,
                ShopRating = rating,
                ProductUrl = url,
                ThumbnailUrl = imageUrl ?? string.Empty,
                SoldCount = soldCount,
                IsOutOfStock = isOutOfStock,
                IsFreeShip = CheckFreeShipping(pageContent)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting product data");
            return null;
        }
    }

    private async Task<string?> ExtractTextAsync(IPage page, string selector)
    {
        try
        {
            var element = await page.QuerySelectorAsync(selector);
            if (element != null)
            {
                var text = await element.TextContentAsync();
                return text?.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract text from selector: {Selector}", selector);
        }
        return null;
    }

    private async Task<decimal> ExtractPriceAsync(IPage page)
    {
        try
        {
            // Try multiple selectors for price
            var priceSelectors = new[]
            {
                "div[class*='pqTWkA']",
                "div.flex.items-center",
                "div[class*='product-price']",
                "span[class*='price']"
            };

            foreach (var selector in priceSelectors)
            {
                var elements = await page.QuerySelectorAllAsync(selector);
                foreach (var element in elements)
                {
                    var text = await element.TextContentAsync();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var price = ParsePrice(text);
                        if (price > 0)
                            return price;
                    }
                }
            }

            // Fallback: search in page content
            var content = await page.ContentAsync();
            var priceMatch = Regex.Match(content, @"₫([\d.,]+)|(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)\s*₫");
            if (priceMatch.Success)
            {
                var priceText = priceMatch.Groups[1].Success ? priceMatch.Groups[1].Value : priceMatch.Groups[2].Value;
                return ParsePrice(priceText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting price");
        }
        return 0;
    }

    private async Task<string?> ExtractImageUrlAsync(IPage page)
    {
        try
        {
            var imgElement = await page.QuerySelectorAsync(IMAGE_SELECTOR);
            if (imgElement != null)
            {
                var src = await imgElement.GetAttributeAsync("src");
                if (!string.IsNullOrWhiteSpace(src))
                    return src;
            }

            // Fallback: try og:image meta tag
            var metaImg = await page.QuerySelectorAsync("meta[property='og:image']");
            if (metaImg != null)
            {
                return await metaImg.GetAttributeAsync("content");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting image URL");
        }
        return null;
    }

    private async Task<bool> CheckOutOfStockAsync(IPage page)
    {
        try
        {
            var content = await page.ContentAsync();
            return content.Contains("Hết hàng", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("Out of stock", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("Tạm hết hàng", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
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
            // Shopee prices are usually in VND, no decimal places
            return price;
        }

        return 0;
    }

    private (decimal? originalPrice, double? discountPercent) ExtractPriceDetails(string content, decimal currentPrice)
    {
        try
        {
            // Look for original price
            var originalPriceMatch = Regex.Match(content, @"price_before_discount[""']?\s*:\s*(\d+)");
            if (originalPriceMatch.Success && long.TryParse(originalPriceMatch.Groups[1].Value, out var originalRaw))
            {
                var original = NormalizeShopeePrice(originalRaw);
                if (original > currentPrice)
                {
                    var discount = (double)((original - currentPrice) / original);
                    return (original, discount);
                }
            }

            // Look for discount percentage
            var discountMatch = Regex.Match(content, @"(\d+)%\s*(?:GIẢM|OFF)", RegexOptions.IgnoreCase);
            if (discountMatch.Success && int.TryParse(discountMatch.Groups[1].Value, out var discountPct))
            {
                var original = currentPrice / (1 - (discountPct / 100m));
                return (original, discountPct / 100.0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting price details");
        }

        return (null, null);
    }

    private int? ExtractSoldCount(string content)
    {
        try
        {
            var soldMatch = Regex.Match(content, @"(?:Đã bán|sold)[^\d]*(\d+(?:[.,]\d+)?)\s*(?:k|K|tr)?", RegexOptions.IgnoreCase);
            if (soldMatch.Success)
            {
                var soldText = soldMatch.Groups[1].Value.Replace(",", "").Replace(".", "");
                if (int.TryParse(soldText, out var sold))
                {
                    // Handle k (thousands) and tr (millions)
                    if (content.Contains("k", StringComparison.OrdinalIgnoreCase))
                        sold *= 1000;
                    else if (content.Contains("tr", StringComparison.OrdinalIgnoreCase))
                        sold *= 1000000;
                    
                    return sold;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting sold count");
        }
        return null;
    }

    private double ExtractRating(string content)
    {
        try
        {
            var ratingMatch = Regex.Match(content, @"rating[""']?\s*:\s*([\d.]+)");
            if (ratingMatch.Success && double.TryParse(ratingMatch.Groups[1].Value, out var rating))
            {
                return rating;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting rating");
        }
        return 0;
    }

    private bool CheckFreeShipping(string content)
    {
        return content.Contains("Miễn phí vận chuyển", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("Free shipping", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("Freeship", StringComparison.OrdinalIgnoreCase);
    }

    private decimal NormalizeShopeePrice(long raw)
    {
        if (raw <= 0) return 0m;
        
        // Shopee stores prices in different formats (x100, x10000, etc.)
        var candidates = new[] { raw, raw / 10, raw / 100, raw / 1000, raw / 10000, raw / 100000 };
        foreach (var c in candidates)
        {
            if (c >= 1_000 && c <= 50_000_000)
                return (decimal)c;
        }
        return (decimal)raw;
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
