using System.Diagnostics;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

/// <summary>
/// Service for searching products across Shopee, Lazada, and Tiki platforms
/// </summary>
public class MultiPlatformSearchService : IMultiPlatformSearchService
{
    private readonly IEnumerable<IProductScraper> _scrapers;
    private readonly ILogger<MultiPlatformSearchService> _logger;
    
    private static readonly Dictionary<string, string> PlatformLogos = new()
    {
        { "shopee", "/images/platforms/shopee-logo.png" },
        { "tiki", "/images/platforms/tiki-logo.png" }
    };

    public MultiPlatformSearchService(
        IEnumerable<IProductScraper> scrapers,
        ILogger<MultiPlatformSearchService> logger)
    {
        _scrapers = scrapers;
        _logger = logger;
    }

    public async Task<MultiPlatformSearchResponse> SearchAsync(
        MultiPlatformSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var response = new MultiPlatformSearchResponse
        {
            Keyword = request.Keyword,
            Metadata = new SearchMetadata()
        };

        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            return response;
        }

        // Determine which platforms to search
        var platformsToSearch = request.Platforms?.Select(p => p.ToLower()).ToList() 
            ?? new List<string> { "shopee", "tiki" };

        var scrapers = _scrapers
            .Where(s => platformsToSearch.Contains(s.Platform.ToLower()))
            .ToList();

        if (!scrapers.Any())
        {
            _logger.LogWarning("No scrapers found for platforms: {Platforms}", 
                string.Join(", ", platformsToSearch));
            return response;
        }

        // Create search query
        var query = new ProductQuery
        {
            Platform = "multi",
            TitleHint = request.Keyword,
            Metadata = new Dictionary<string, string>
            {
                { "limit", request.Limit.ToString() },
                { "offset", request.Offset.ToString() }
            }
        };

        // Search all platforms in parallel
        var searchTasks = scrapers.Select(async scraper =>
        {
            var platformSw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Searching {Platform} for: {Keyword}", 
                    scraper.Platform, request.Keyword);

                var results = await scraper.SearchByQueryAsync(query, cancellationToken);
                var products = results.Select(r => MapToDto(r, scraper.Platform)).ToList();

                platformSw.Stop();
                response.Metadata.PlatformDurations[scraper.Platform] = platformSw.ElapsedMilliseconds;

                _logger.LogInformation("Found {Count} products on {Platform} in {Ms}ms",
                    products.Count, scraper.Platform, platformSw.ElapsedMilliseconds);

                return (Platform: scraper.Platform, Products: products, Error: (string?)null);
            }
            catch (Exception ex)
            {
                platformSw.Stop();
                _logger.LogError(ex, "Error searching {Platform}", scraper.Platform);
                response.Metadata.PlatformErrors[scraper.Platform] = ex.Message;
                return (Platform: scraper.Platform, Products: new List<PlatformProductDto>(), Error: ex.Message);
            }
        }).ToList();

        var results = await Task.WhenAll(searchTasks);

        // Combine and process results
        var allProducts = new List<PlatformProductDto>();
        foreach (var (platform, products, error) in results)
        {
            response.ResultsByPlatform[platform] = products.Count;
            allProducts.AddRange(products);
        }

        // Apply filters
        allProducts = ApplyFilters(allProducts, request.Filters);

        // Apply sorting
        allProducts = ApplySorting(allProducts, request.SortBy);

        response.Products = allProducts;
        response.TotalResults = allProducts.Count;

        sw.Stop();
        response.Metadata.SearchDurationMs = sw.ElapsedMilliseconds;

        _logger.LogInformation("Multi-platform search completed: {Total} products from {Platforms} platforms in {Ms}ms",
            response.TotalResults, response.ResultsByPlatform.Count, sw.ElapsedMilliseconds);

        return response;
    }

    public async Task<List<PriceComparisonDto>> ComparePricesAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = new MultiPlatformSearchRequest
        {
            Keyword = keyword,
            Limit = 10
        };

        var searchResults = await SearchAsync(searchRequest, cancellationToken);

        // Group similar products by normalized name
        var productGroups = searchResults.Products
            .GroupBy(p => NormalizeProductName(p.ProductName))
            .Where(g => g.Count() > 1) // Only products found on multiple platforms
            .Select(g => new PriceComparisonDto
            {
                ProductName = g.First().ProductName,
                Prices = g.Select(p => new PlatformPriceDto
                {
                    Platform = p.Platform,
                    Price = p.Price,
                    ShippingCost = p.ShippingCost,
                    TotalCost = p.Price + p.ShippingCost,
                    Rating = p.Rating,
                    ProductUrl = p.ProductUrl,
                    ShopName = p.ShopName
                }).ToList()
            })
            .ToList();

        // Calculate price statistics
        foreach (var comparison in productGroups)
        {
            var prices = comparison.Prices.Select(p => p.TotalCost).ToList();
            comparison.LowestPrice = prices.Min();
            comparison.HighestPrice = prices.Max();
            comparison.AveragePrice = prices.Average();
            comparison.BestDealPlatform = comparison.Prices
                .OrderBy(p => p.TotalCost)
                .First()
                .Platform;
        }

        return productGroups;
    }

    #region Private Helper Methods

    private PlatformProductDto MapToDto(ProductCandidateDto candidate, string platform)
    {
        return new PlatformProductDto
        {
            ProductName = candidate.Title,
            ProductImage = candidate.ThumbnailUrl,
            ProductUrl = candidate.ProductUrl,
            Price = candidate.Price,
            PriceBeforeDiscount = candidate.OriginalPrice,
            DiscountPercent = candidate.DiscountPercent,
            Rating = candidate.ShopRating > 0 ? candidate.ShopRating : null,
            ReviewCount = null, // Can be enhanced
            SoldCount = candidate.SoldCount,
            ShopName = candidate.ShopName,
            Platform = platform,
            PlatformLogo = PlatformLogos.GetValueOrDefault(platform.ToLower(), ""),
            IsFreeShip = candidate.IsFreeShip ?? false,
            IsOfficialStore = candidate.SellerType?.Contains("Chính hãng") ?? false,
            SellerType = candidate.SellerType,
            ShippingCost = candidate.ShippingCost,
            IsOutOfStock = candidate.IsOutOfStock ?? false
        };
    }

    private List<PlatformProductDto> ApplyFilters(
        List<PlatformProductDto> products,
        MultiPlatformSearchFilters? filters)
    {
        if (filters == null)
            return products;

        var filtered = products.AsEnumerable();

        if (filters.MinPrice.HasValue)
            filtered = filtered.Where(p => p.Price >= filters.MinPrice.Value);

        if (filters.MaxPrice.HasValue)
            filtered = filtered.Where(p => p.Price <= filters.MaxPrice.Value);

        if (filters.MinRating.HasValue)
            filtered = filtered.Where(p => p.Rating >= filters.MinRating.Value);

        if (filters.FreeShipping == true)
            filtered = filtered.Where(p => p.IsFreeShip);

        if (filters.OfficialStore == true)
            filtered = filtered.Where(p => p.IsOfficialStore);

        return filtered.ToList();
    }

    private List<PlatformProductDto> ApplySorting(
        List<PlatformProductDto> products,
        string? sortBy)
    {
        return sortBy?.ToLower() switch
        {
            "price_asc" => products.OrderBy(p => p.Price).ToList(),
            "price_desc" => products.OrderByDescending(p => p.Price).ToList(),
            "rating" => products.OrderByDescending(p => p.Rating ?? 0).ToList(),
            "sold" => products.OrderByDescending(p => p.SoldCount ?? 0).ToList(),
            "discount" => products.OrderByDescending(p => p.DiscountPercent ?? 0).ToList(),
            _ => products // Default: relevance (as returned by platforms)
        };
    }

    private string NormalizeProductName(string name)
    {
        // Remove special characters, extra spaces, and convert to lowercase
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            name.ToLower(),
            @"[^\w\s]",
            " "
        );
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();
        
        // Take first 50 characters for grouping
        return normalized.Length > 50 ? normalized.Substring(0, 50) : normalized;
    }

    #endregion
}
