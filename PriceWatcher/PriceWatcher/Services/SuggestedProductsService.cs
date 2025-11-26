using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class SuggestedProductsService : ISuggestedProductsService
{
    private readonly PriceWatcherDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IEnumerable<IProductScraper> _scrapers;
    private readonly ILogger<SuggestedProductsService> _logger;
    private const string CACHE_KEY_ALL = "suggested_products_all";
    private const string CACHE_KEY_PLATFORM_PREFIX = "suggested_products_";
    private const int CACHE_DURATION_MINUTES = 30;

    public SuggestedProductsService(
        PriceWatcherDbContext context,
        IMemoryCache cache,
        IEnumerable<IProductScraper> scrapers,
        ILogger<SuggestedProductsService> logger)
    {
        _context = context;
        _cache = cache;
        _scrapers = scrapers;
        _logger = logger;
    }

    public async Task<List<SuggestedProductDto>> GetSuggestedProductsAsync(int limit = 12, CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_cache.TryGetValue(CACHE_KEY_ALL, out List<SuggestedProductDto>? cachedProducts) && cachedProducts != null)
        {
            _logger.LogInformation("Returning {Count} suggested products from cache", cachedProducts.Count);
            return cachedProducts.Take(limit).ToList();
        }

        var allProducts = new List<SuggestedProductDto>();

        // Get products from database (already tracked products)
        var dbProducts = await GetProductsFromDatabaseAsync(limit, cancellationToken);
        allProducts.AddRange(dbProducts);

        // If we don't have enough products from DB, try to get from scrapers
        if (allProducts.Count < limit)
        {
            var scrapedProducts = await GetProductsFromScrapersAsync(limit - allProducts.Count, cancellationToken);
            allProducts.AddRange(scrapedProducts);
        }

        // Shuffle and take the requested limit
        var shuffled = allProducts
            .OrderBy(x => Guid.NewGuid())
            .Take(limit)
            .ToList();

        // Cache the results
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

        _cache.Set(CACHE_KEY_ALL, shuffled, cacheOptions);

        _logger.LogInformation("Returning {Count} suggested products (cached for {Minutes} minutes)", shuffled.Count, CACHE_DURATION_MINUTES);
        return shuffled;
    }

    public async Task<List<SuggestedProductDto>> GetSuggestedProductsByPlatformAsync(string platformName, int limit = 4, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CACHE_KEY_PLATFORM_PREFIX}{platformName.ToLower()}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out List<SuggestedProductDto>? cachedProducts) && cachedProducts != null)
        {
            _logger.LogInformation("Returning {Count} suggested products for {Platform} from cache", cachedProducts.Count, platformName);
            return cachedProducts.Take(limit).ToList();
        }

        var products = new List<SuggestedProductDto>();

        // Get from database first
        var dbProducts = await _context.Products
            .Include(p => p.Platform)
            .Where(p => p.Platform != null && p.Platform.PlatformName.ToLower() == platformName.ToLower())
            .Where(p => p.CurrentPrice.HasValue && p.CurrentPrice > 0)
            .OrderByDescending(p => p.LastUpdated)
            .Take(limit)
            .Select(p => new SuggestedProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ImageUrl = p.ImageUrl ?? string.Empty,
                Price = p.CurrentPrice ?? 0,
                OriginalPrice = p.OriginalPrice,
                DiscountRate = p.DiscountRate,
                ProductUrl = p.OriginalUrl,
                Platform = p.Platform!.PlatformName,
                PlatformId = p.PlatformId ?? 0,
                PlatformLogo = $"/images/platforms/{p.Platform.PlatformName.ToLower()}.png",
                PlatformColor = p.Platform.ColorCode ?? "#000000",
                Rating = p.Rating,
                ReviewCount = p.ReviewCount,
                ShopName = p.ShopName,
                IsFreeShip = p.ShippingInfo != null && p.ShippingInfo.Contains("Free", StringComparison.OrdinalIgnoreCase),
                LastUpdated = p.LastUpdated ?? DateTime.Now
            })
            .ToListAsync(cancellationToken);

        products.AddRange(dbProducts);

        // If we don't have enough from DB, try scraper
        if (products.Count < limit)
        {
            var scraper = _scrapers.FirstOrDefault(s => s.Platform.Equals(platformName, StringComparison.OrdinalIgnoreCase));
            if (scraper != null)
            {
                try
                {
                    var categories = new[] { "điện thoại", "laptop", "máy ảnh", "âm thanh", "thời trang" };
                    foreach (var cat in categories)
                    {
                        var scrapedProducts = await GetProductsFromScraperAsync(scraper, cat, pageCount: 1, pageSize: limit, cancellationToken);
                        products.AddRange(scrapedProducts);
                        if (products.Count >= limit) break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scrape products from {Platform}", platformName);
                }
            }
        }

        // Cache the results
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

        _cache.Set(cacheKey, products, cacheOptions);

        return products.Take(limit).ToList();
    }

    public async Task<List<SuggestedProductDto>> GetCategoryCrawlAsync(string[] categories, int perCategory = 12, CancellationToken cancellationToken = default)
    {
        var results = new List<SuggestedProductDto>();
        foreach (var scraper in _scrapers)
        {
            foreach (var cat in categories)
            {
                try
                {
                    var products = await GetProductsFromScraperAsync(scraper, cat, pageCount: 2, pageSize: perCategory, cancellationToken);
                    results.AddRange(products);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Category crawl failed for {Platform} - {Category}", scraper.Platform, cat);
                }
            }
        }
        // Persist
        await UpsertProductsAsync(results, cancellationToken);
        return results;
    }

    private async Task<List<SuggestedProductDto>> GetProductsFromDatabaseAsync(int limit, CancellationToken cancellationToken)
    {
        try
        {
            // Get products from all platforms, distributed evenly
            var productsPerPlatform = (int)Math.Ceiling(limit / 3.0);

            var products = await _context.Products
                .Include(p => p.Platform)
                .Where(p => p.Platform != null)
                .Where(p => p.CurrentPrice.HasValue && p.CurrentPrice > 0)
                .Where(p => p.Platform.PlatformName == "Tiki" || p.Platform.PlatformName == "Shopee" || p.Platform.PlatformName == "Lazada")
                .GroupBy(p => p.Platform!.PlatformName)
                .SelectMany(g => g.OrderByDescending(p => p.LastUpdated).Take(productsPerPlatform))
                .Select(p => new SuggestedProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ImageUrl = p.ImageUrl ?? string.Empty,
                    Price = p.CurrentPrice ?? 0,
                    OriginalPrice = p.OriginalPrice,
                    DiscountRate = p.DiscountRate,
                    ProductUrl = p.OriginalUrl,
                    Platform = p.Platform!.PlatformName,
                    PlatformId = p.PlatformId ?? 0,
                    PlatformLogo = $"/images/platforms/{p.Platform.PlatformName.ToLower()}.png",
                    PlatformColor = p.Platform.ColorCode ?? "#000000",
                    Rating = p.Rating,
                    ReviewCount = p.ReviewCount,
                    ShopName = p.ShopName,
                    IsFreeShip = p.ShippingInfo != null && p.ShippingInfo.Contains("Free", StringComparison.OrdinalIgnoreCase),
                    LastUpdated = p.LastUpdated ?? DateTime.Now
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} products from database", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products from database");
            return new List<SuggestedProductDto>();
        }
    }

    private async Task<List<SuggestedProductDto>> GetProductsFromScrapersAsync(int limit, CancellationToken cancellationToken)
    {
        var allProducts = new List<SuggestedProductDto>();
        var categories = new[]
        {
            "điện thoại",
            "laptop",
            "máy ảnh",
            "âm thanh",
            "đồng hồ",
            "nhà cửa",
            "thời trang",
        };

        foreach (var scraper in _scrapers)
        {
            if (cancellationToken.IsCancellationRequested) break;

            foreach (var cat in categories)
            {
                if (cancellationToken.IsCancellationRequested) break;
                try
                {
                    var products = await GetProductsFromScraperAsync(scraper, cat, pageCount: 2, pageSize: 20, cancellationToken);
                    allProducts.AddRange(products);
                    _logger.LogInformation("{Platform} - Collected {Count} items for category {Category}", scraper.Platform, products.Count, cat);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Platform} - Failed category {Category}", scraper.Platform, cat);
                }
            }
        }

        // Shuffle to diversify results across categories/platforms
        return allProducts.OrderBy(x => Guid.NewGuid()).Take(limit).ToList();
    }

    private async Task<List<SuggestedProductDto>> GetProductsFromScraperAsync(IProductScraper scraper, string categoryKeyword, int pageCount, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var collected = new List<ProductCandidateDto>();
            for (var page = 1; page <= pageCount; page++)
            {
                var query = new ProductQuery
                {
                    TitleHint = categoryKeyword,
                    Metadata = new Dictionary<string, string>
                    {
                        ["page"] = page.ToString(),
                        ["limit"] = pageSize.ToString(),
                        ["offset"] = ((page - 1) * pageSize).ToString()
                    }
                };
                var batch = await scraper.SearchByQueryAsync(query, cancellationToken);
                collected.AddRange(batch);
            }

            // Get platform info from database
            var platform = await _context.Platforms
                .FirstOrDefaultAsync(p => p.PlatformName == scraper.Platform, cancellationToken);

            var products = collected
                .Where(c => !string.IsNullOrWhiteSpace(c.Title))
                .Where(c => c.Price > 0)
                .Where(c => !string.IsNullOrWhiteSpace(c.ProductUrl))
                .Where(c => !string.IsNullOrWhiteSpace(c.ThumbnailUrl))
                .Select(c => new SuggestedProductDto
                {
                    ProductId = c.ProductId,
                    ProductName = c.Title,
                    ImageUrl = c.ThumbnailUrl ?? string.Empty,
                    Price = c.Price,
                    OriginalPrice = c.OriginalPrice,
                    DiscountRate = c.DiscountPercent.HasValue ? (int)Math.Round(c.DiscountPercent.Value * 100) : null,
                    ProductUrl = c.ProductUrl ?? string.Empty,
                    Platform = scraper.Platform,
                    PlatformId = platform?.PlatformId ?? 0,
                    PlatformLogo = $"/images/platforms/{scraper.Platform.ToLower()}.png",
                    PlatformColor = platform?.ColorCode ?? "#000000",
                    Rating = c.ShopRating,
                    ReviewCount = c.ShopSales,
                    ShopName = c.ShopName,
                    IsFreeShip = c.IsFreeShip ?? false,
                    LastUpdated = DateTime.Now
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} products from {Platform} scraper for {Category}", products.Count, scraper.Platform, categoryKeyword);
            await UpsertProductsAsync(products, cancellationToken);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping products from {Platform}", scraper.Platform);
            return new List<SuggestedProductDto>();
        }
    }

    private async Task UpsertProductsAsync(IEnumerable<SuggestedProductDto> items, CancellationToken ct)
    {
        try
        {
            foreach (var dto in items)
            {
                if (string.IsNullOrWhiteSpace(dto.ProductUrl)) continue;
                var product = await _context.Products.FirstOrDefaultAsync(p => p.OriginalUrl == dto.ProductUrl, ct);
                if (product == null)
                {
                    product = new Product
                    {
                        ProductName = dto.ProductName,
                        OriginalUrl = dto.ProductUrl,
                        ImageUrl = dto.ImageUrl,
                        CurrentPrice = dto.Price,
                        OriginalPrice = dto.OriginalPrice,
                        DiscountRate = dto.DiscountRate,
                        Rating = dto.Rating,
                        ReviewCount = dto.ReviewCount,
                        ShopName = dto.ShopName,
                        LastUpdated = DateTime.UtcNow
                    };
                    if (dto.PlatformId != 0) product.PlatformId = dto.PlatformId;
                    else
                    {
                        var platform = await _context.Platforms.FirstOrDefaultAsync(p => p.PlatformName == dto.Platform, ct);
                        if (platform != null) product.PlatformId = platform.PlatformId;
                    }
                    await _context.Products.AddAsync(product, ct);
                }
                else
                {
                    product.ProductName = dto.ProductName;
                    product.ImageUrl = dto.ImageUrl;
                    product.CurrentPrice = dto.Price;
                    product.OriginalPrice = dto.OriginalPrice;
                    product.DiscountRate = dto.DiscountRate;
                    product.Rating = dto.Rating;
                    product.ReviewCount = dto.ReviewCount;
                    product.ShopName = dto.ShopName;
                    product.LastUpdated = DateTime.UtcNow;
                }
            }
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpsertProductsAsync failed");
        }
    }
}
