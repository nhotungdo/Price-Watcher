using Microsoft.EntityFrameworkCore;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

/// <summary>
/// Core service for product tracking and price history management
/// </summary>
public class ProductService : IProductService
{
    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILinkProcessor _linkProcessor;
    private readonly IEnumerable<IProductScraper> _scrapers;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        PriceWatcherDbContext dbContext,
        ILinkProcessor linkProcessor,
        IEnumerable<IProductScraper> scrapers,
        ILogger<ProductService> logger)
    {
        _dbContext = dbContext;
        _linkProcessor = linkProcessor;
        _scrapers = scrapers;
        _logger = logger;
    }

    public async Task<ProductTrackingResultDto> TrackProductByUrlAsync(
        string url,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tracking product URL: {Url} for user: {UserId}", url, userId);

        // Step 1: Parse URL to get platform and product info
        ProductQuery query;
        try
        {
            query = await _linkProcessor.ProcessUrlAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process URL: {Url}", url);
            throw new InvalidOperationException("Invalid or unsupported product URL", ex);
        }

        // Step 2: Check if product already exists in database
        var existingProduct = await FindProductByQueryAsync(query, cancellationToken);

        Product product;
        bool isNewProduct;
        bool isPriceUpdated = false;

        if (existingProduct != null)
        {
            // Product exists - scrape latest data and update
            _logger.LogInformation("Product exists (ID: {ProductId}), updating...", existingProduct.ProductId);
            
            product = existingProduct;
            isNewProduct = false;

            // Scrape latest data
            var scrapedData = await ScrapeProductDataAsync(query, cancellationToken);
            if (scrapedData != null)
            {
                // Check if price changed
                var priceChanged = product.CurrentPrice != scrapedData.Price;
                
                // Update product details
                UpdateProductFromScrapedData(product, scrapedData);
                product.LastUpdated = DateTime.UtcNow;

                // Add new price snapshot if price changed or it's been more than 1 hour
                var lastSnapshot = await _dbContext.PriceSnapshots
                    .Where(ps => ps.ProductId == product.ProductId)
                    .OrderByDescending(ps => ps.RecordedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                var shouldAddSnapshot = priceChanged || 
                    lastSnapshot == null || 
                    (DateTime.UtcNow - lastSnapshot.RecordedAt.GetValueOrDefault()) > TimeSpan.FromHours(1);

                if (shouldAddSnapshot)
                {
                    await AddPriceSnapshotAsync(product.ProductId, scrapedData.Price, scrapedData.OriginalPrice, cancellationToken);
                    isPriceUpdated = true;
                    _logger.LogInformation("Added new price snapshot for product {ProductId}: {Price}", 
                        product.ProductId, scrapedData.Price);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            // New product - scrape and create
            _logger.LogInformation("New product, scraping data...");
            
            var scrapedData = await ScrapeProductDataAsync(query, cancellationToken);
            if (scrapedData == null)
            {
                throw new InvalidOperationException("Failed to scrape product data");
            }

            // Create new product
            product = CreateProductFromScrapedData(query, scrapedData);
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync(cancellationToken);

            isNewProduct = true;
            isPriceUpdated = true;

            // Add initial price snapshot
            await AddPriceSnapshotAsync(product.ProductId, scrapedData.Price, scrapedData.OriginalPrice, cancellationToken);
            
            _logger.LogInformation("Created new product (ID: {ProductId}): {ProductName}", 
                product.ProductId, product.ProductName);
        }

        // Step 3: Save to search history if user is logged in
        if (userId.HasValue)
        {
            await SaveSearchHistoryAsync(userId.Value, url, query.Platform, product.ProductName, product.CurrentPrice, cancellationToken);
        }

        // Step 4: Get price history and return result
        var priceHistory = await GetPriceHistoryChartAsync(product.ProductId, cancellationToken);

        return new ProductTrackingResultDto
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Platform = query.Platform,
            OriginalUrl = product.OriginalUrl,
            ImageUrl = product.ImageUrl,
            CurrentPrice = product.CurrentPrice ?? 0,
            OriginalPrice = product.OriginalPrice,
            DiscountRate = product.DiscountRate,
            ShopName = product.ShopName,
            Rating = product.Rating,
            ReviewCount = product.ReviewCount,
            StockStatus = product.StockStatus,
            IsNewProduct = isNewProduct,
            IsPriceUpdated = isPriceUpdated,
            PriceHistory = priceHistory
        };
    }

    public async Task<ProductWithHistoryDto?> GetProductWithHistoryAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .Include(p => p.Platform)
            .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

        if (product == null)
        {
            return null;
        }

        var priceHistory = await GetPriceHistoryChartAsync(productId, cancellationToken);

        return new ProductWithHistoryDto
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Platform = product.Platform?.PlatformName ?? "Unknown",
            OriginalUrl = product.OriginalUrl,
            ImageUrl = product.ImageUrl,
            CurrentPrice = product.CurrentPrice ?? 0,
            OriginalPrice = product.OriginalPrice,
            DiscountRate = product.DiscountRate,
            ShopName = product.ShopName,
            Rating = product.Rating,
            ReviewCount = product.ReviewCount,
            SoldQuantity = product.SoldQuantity,
            StockStatus = product.StockStatus,
            LastUpdated = product.LastUpdated,
            PriceHistory = priceHistory
        };
    }

    public async Task<int?> FindProductByUrlAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = await _linkProcessor.ProcessUrlAsync(url, cancellationToken);
            var product = await FindProductByQueryAsync(query, cancellationToken);
            return product?.ProductId;
        }
        catch
        {
            return null;
        }
    }

    #region Private Helper Methods

    private async Task<Product?> FindProductByQueryAsync(
        ProductQuery query,
        CancellationToken cancellationToken)
    {
        // Try to find by canonical URL first
        var product = await _dbContext.Products
            .Include(p => p.Platform)
            .FirstOrDefaultAsync(p => p.OriginalUrl == query.CanonicalUrl, cancellationToken);

        if (product != null)
        {
            return product;
        }

        // Try to find by external ID and platform
        if (!string.IsNullOrWhiteSpace(query.ProductId))
        {
            var platform = await _dbContext.Platforms
                .FirstOrDefaultAsync(p => p.PlatformName.ToLower() == query.Platform.ToLower(), cancellationToken);

            if (platform != null)
            {
                product = await _dbContext.Products
                    .Include(p => p.Platform)
                    .FirstOrDefaultAsync(p => 
                        p.ExternalId == query.ProductId && 
                        p.PlatformId == platform.PlatformId, 
                        cancellationToken);
            }
        }

        return product;
    }

    private async Task<ProductCandidateDto?> ScrapeProductDataAsync(
        ProductQuery query,
        CancellationToken cancellationToken)
    {
        var scraper = _scrapers.FirstOrDefault(s => 
            s.Platform.Equals(query.Platform, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            _logger.LogWarning("No scraper found for platform: {Platform}", query.Platform);
            return null;
        }

        try
        {
            return await scraper.GetByUrlAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrape product from {Platform}", query.Platform);
            return null;
        }
    }

    private Product CreateProductFromScrapedData(ProductQuery query, ProductCandidateDto data)
    {
        var platform = _dbContext.Platforms
            .FirstOrDefault(p => p.PlatformName.ToLower() == query.Platform.ToLower());

        return new Product
        {
            PlatformId = platform?.PlatformId,
            ExternalId = query.ProductId,
            ProductName = data.Title,
            OriginalUrl = query.CanonicalUrl ?? data.ProductUrl,
            ImageUrl = data.ThumbnailUrl,
            CurrentPrice = data.Price,
            OriginalPrice = data.OriginalPrice,
            DiscountRate = data.DiscountPercent.HasValue ? (int)(data.DiscountPercent.Value * 100) : null,
            ShopName = data.ShopName,
            Rating = data.ShopRating,
            ReviewCount = null,
            SoldQuantity = data.SoldCount,
            StockStatus = data.IsOutOfStock == true ? "OutOfStock" : "InStock",
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
    }

    private void UpdateProductFromScrapedData(Product product, ProductCandidateDto data)
    {
        product.ProductName = data.Title;
        product.CurrentPrice = data.Price;
        product.OriginalPrice = data.OriginalPrice;
        product.DiscountRate = data.DiscountPercent.HasValue ? (int)(data.DiscountPercent.Value * 100) : null;
        product.ImageUrl = data.ThumbnailUrl ?? product.ImageUrl;
        product.ShopName = data.ShopName ?? product.ShopName;
        product.Rating = data.ShopRating > 0 ? data.ShopRating : product.Rating;
        product.SoldQuantity = data.SoldCount ?? product.SoldQuantity;
        product.StockStatus = data.IsOutOfStock == true ? "OutOfStock" : "InStock";
    }

    private async Task AddPriceSnapshotAsync(
        int productId,
        decimal price,
        decimal? originalPrice,
        CancellationToken cancellationToken)
    {
        var snapshot = new PriceSnapshot
        {
            ProductId = productId,
            Price = price,
            OriginalPrice = originalPrice,
            ShippingInfo = "Standard", // Can be enhanced later
            RecordedAt = DateTime.UtcNow
        };

        _dbContext.PriceSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveSearchHistoryAsync(
        int userId,
        string url,
        string platform,
        string productName,
        decimal? price,
        CancellationToken cancellationToken)
    {
        try
        {
            var searchHistory = new SearchHistory
            {
                UserId = userId,
                SearchType = "Link",
                InputContent = url,
                DetectedKeyword = productName,
                BestPriceFound = price,
                SearchTime = DateTime.UtcNow
            };

            _dbContext.SearchHistories.Add(searchHistory);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Saved search history for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save search history for user {UserId}", userId);
            // Don't throw - search history is not critical
        }
    }

    private async Task<PriceHistoryChartDto> GetPriceHistoryChartAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.PriceSnapshots
            .Where(ps => ps.ProductId == productId)
            .OrderBy(ps => ps.RecordedAt)
            .Select(ps => new PricePointDto
            {
                RecordedAt = ps.RecordedAt ?? DateTime.UtcNow,
                Price = ps.Price,
                OriginalPrice = ps.OriginalPrice
            })
            .ToListAsync(cancellationToken);

        if (!snapshots.Any())
        {
            return new PriceHistoryChartDto();
        }

        var prices = snapshots.Select(s => s.Price).ToList();
        var lowestPrice = prices.Min();
        var highestPrice = prices.Max();
        var averagePrice = prices.Average();

        var firstPrice = snapshots.First().Price;
        var lastPrice = snapshots.Last().Price;
        var priceChange = lastPrice - firstPrice;
        var priceChangePercent = firstPrice > 0 ? (double)((priceChange / firstPrice) * 100) : 0;

        return new PriceHistoryChartDto
        {
            DataPoints = snapshots,
            LowestPrice = lowestPrice,
            HighestPrice = highestPrice,
            AveragePrice = averagePrice,
            PriceChange = priceChange,
            PriceChangePercent = priceChangePercent,
            TotalSnapshots = snapshots.Count
        };
    }

    #endregion
}
