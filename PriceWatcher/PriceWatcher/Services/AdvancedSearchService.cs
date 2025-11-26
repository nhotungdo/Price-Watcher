using Microsoft.EntityFrameworkCore;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace PriceWatcher.Services;

public class AdvancedSearchService : IAdvancedSearchService
{
    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILogger<AdvancedSearchService> _logger;
    private static readonly Regex CollapseSpacesRegex = new(@"\s+", RegexOptions.Compiled);

    public AdvancedSearchService(
        PriceWatcherDbContext dbContext,
        ILogger<AdvancedSearchService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SearchResult> SearchWithFiltersAsync(SearchFilters filters, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking();

        // Apply keyword filter
        if (!string.IsNullOrWhiteSpace(filters.Keyword))
        {
            var normalized = Normalize(filters.Keyword);
            var tokens = Tokenize(normalized);
            if (tokens.Length > 0)
            {
                var baseToken = tokens[0];
                query = query.Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{baseToken}%"));
            }
        }

        // Apply price filters
        if (filters.MinPrice.HasValue)
        {
            query = query.Where(p => p.CurrentPrice >= filters.MinPrice.Value);
        }

        if (filters.MaxPrice.HasValue)
        {
            query = query.Where(p => p.CurrentPrice <= filters.MaxPrice.Value);
        }

        // Apply category filter
        if (filters.CategoryIds?.Any() == true)
        {
            query = query.Where(p => p.CategoryId.HasValue && filters.CategoryIds.Contains(p.CategoryId.Value));
        }

        // Apply platform filter
        if (filters.PlatformIds?.Any() == true)
        {
            query = query.Where(p => p.PlatformId.HasValue && filters.PlatformIds.Contains(p.PlatformId.Value));
        }

        // Apply rating filter
        if (filters.MinRating.HasValue)
        {
            query = query.Where(p => p.Rating >= (double)filters.MinRating.Value);
        }

        // Apply free shipping filter
        if (filters.FreeShippingOnly)
        {
            query = query.Where(p => p.IsFreeShip == true);
        }

        // Apply verified stores filter
        if (filters.VerifiedStoresOnly)
        {
            query = query.Where(p => p.IsVerified == true);
        }

        // Apply sorting
        query = filters.SortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.CurrentPrice ?? decimal.MaxValue),
            "price_desc" => query.OrderByDescending(p => p.CurrentPrice ?? 0),
            "rating" => query.OrderByDescending(p => p.Rating ?? 0),
            "popularity" => query.OrderByDescending(p => p.ReviewCount ?? 0),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.LastUpdated ?? DateTime.MinValue)
        };

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(p => new ProductSearchItemDto
            {
                ProductId = p.ProductId,
                Title = p.ProductName ?? "",
                HighlightedTitle = p.ProductName ?? "",
                Platform = p.Platform != null ? p.Platform.PlatformName : null,
                ShopName = p.ShopName,
                ImageUrl = p.ImageUrl,
                ProductUrl = p.OriginalUrl,
                Price = p.CurrentPrice,
                OriginalPrice = p.OriginalPrice,
                Rating = p.Rating,
                ReviewCount = p.ReviewCount,
                IsFreeShip = p.IsFreeShip ?? false,
                IsVerified = p.IsVerified ?? false,
                DiscountPercent = p.OriginalPrice.HasValue && p.CurrentPrice.HasValue && p.OriginalPrice > p.CurrentPrice
                    ? (int)Math.Round(((p.OriginalPrice.Value - p.CurrentPrice.Value) / p.OriginalPrice.Value) * 100)
                    : 0
            })
            .ToListAsync(cancellationToken);

        // Get aggregations
        var categoryCounts = await GetCategoryCountsAsync(filters.Keyword, cancellationToken);
        var platformCounts = await GetPlatformCountsAsync(filters.Keyword, cancellationToken);
        var priceRange = await GetPriceRangeAsync(filters.Keyword, cancellationToken);

        return new SearchResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = filters.Page,
            PageSize = filters.PageSize,
            AppliedFilters = filters,
            CategoryCounts = categoryCounts,
            PlatformCounts = platformCounts,
            MinPriceFound = priceRange.min,
            MaxPriceFound = priceRange.max
        };
    }

    public async Task<List<string>> GetSearchSuggestionsAsync(string keyword, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return new List<string>();
        }

        var normalized = Normalize(keyword);
        var tokens = Tokenize(normalized);
        if (tokens.Length == 0)
        {
            return new List<string>();
        }

        var baseToken = tokens[0];

        var suggestions = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{baseToken}%"))
            .Select(p => p.ProductName!)
            .Distinct()
            .Take(limit * 2)
            .ToListAsync(cancellationToken);

        // Score and rank suggestions
        var scored = suggestions
            .Select(s => new { Name = s, Score = ComputeScore(tokens, Normalize(s)) })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => x.Name)
            .ToList();

        return scored;
    }

    public async Task<Dictionary<string, int>> GetCategoryCountsAsync(string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = Normalize(keyword);
            var tokens = Tokenize(normalized);
            if (tokens.Length > 0)
            {
                var baseToken = tokens[0];
                query = query.Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{baseToken}%"));
            }
        }

        var counts = await query
            .Where(p => p.Category != null)
            .GroupBy(p => p.Category!.CategoryName)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count, cancellationToken);

        return counts;
    }

    public async Task<Dictionary<string, int>> GetPlatformCountsAsync(string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = Normalize(keyword);
            var tokens = Tokenize(normalized);
            if (tokens.Length > 0)
            {
                var baseToken = tokens[0];
                query = query.Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{baseToken}%"));
            }
        }

        var counts = await query
            .Where(p => p.Platform != null)
            .GroupBy(p => p.Platform!.PlatformName)
            .Select(g => new { Platform = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Platform, x => x.Count, cancellationToken);

        return counts;
    }

    public async Task<(decimal? min, decimal? max)> GetPriceRangeAsync(string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking().Where(p => p.CurrentPrice.HasValue);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = Normalize(keyword);
            var tokens = Tokenize(normalized);
            if (tokens.Length > 0)
            {
                var baseToken = tokens[0];
                query = query.Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{baseToken}%"));
            }
        }

        var min = await query.MinAsync(p => p.CurrentPrice, cancellationToken);
        var max = await query.MaxAsync(p => p.CurrentPrice, cancellationToken);

        return (min, max);
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(capacity: normalized.Length);
        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        result = CollapseSpacesRegex.Replace(result, " ").Trim();
        return result;
    }

    private static string[] Tokenize(string normalizedText)
        => string.IsNullOrWhiteSpace(normalizedText)
            ? Array.Empty<string>()
            : normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

    private static decimal ComputeScore(string[] tokens, string normalizedCandidate)
    {
        if (tokens.Length == 0 || string.IsNullOrWhiteSpace(normalizedCandidate))
        {
            return 0m;
        }

        var candidateTokens = Tokenize(normalizedCandidate);
        if (candidateTokens.Length == 0)
        {
            return 0m;
        }

        var intersection = tokens.Intersect(candidateTokens).Count();
        var union = tokens.Union(candidateTokens).Count();
        var jaccard = union == 0 ? 0m : (decimal)intersection / union;
        var joined = string.Join(' ', tokens);
        var prefix = normalizedCandidate.StartsWith(joined, StringComparison.Ordinal) ? 0.3m : 0m;
        var contains = normalizedCandidate.Contains(joined, StringComparison.Ordinal) ? 0.2m : 0m;
        return Math.Min(1m, jaccard + prefix + contains);
    }
}
