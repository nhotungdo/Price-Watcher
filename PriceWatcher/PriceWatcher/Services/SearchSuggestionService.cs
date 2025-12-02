using Microsoft.EntityFrameworkCore;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;
using System.Text.RegularExpressions;

namespace PriceWatcher.Services;

public class SearchSuggestionService : ISearchSuggestionService
{
    private readonly PriceWatcherDbContext _context;
    private readonly ILogger<SearchSuggestionService> _logger;

    public SearchSuggestionService(
        PriceWatcherDbContext context,
        ILogger<SearchSuggestionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SearchSuggestionsResponse> GetSuggestionsAsync(
        string query, 
        int? userId, 
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        var response = new SearchSuggestionsResponse { Success = true };

        if (string.IsNullOrWhiteSpace(query))
        {
            // Return trending and recent searches when query is empty
            response.DetectedType = "empty";
            response.TrendingKeywords = await GetTrendingSuggestionsAsync(5, cancellationToken);
            
            if (userId.HasValue)
            {
                response.RecentSearches = await GetRecentSearchSuggestionsAsync(userId.Value, 5, cancellationToken);
            }
            
            return response;
        }

        query = query.Trim();

        // Detect if query is a URL
        if (IsUrl(query))
        {
            response.DetectedType = "url";
            response.Suggestions = new List<SearchSuggestionDto>
            {
                new SearchSuggestionDto
                {
                    Type = "url",
                    Text = "Tìm kiếm sản phẩm từ link",
                    SecondaryText = query,
                    Url = $"/MultiSearch?url={Uri.EscapeDataString(query)}"
                }
            };
            return response;
        }

        response.DetectedType = "text";

        // Search products by name
        var productSuggestions = await SearchProductsAsync(query, limit / 2, cancellationToken);
        response.Suggestions.AddRange(productSuggestions);

        // Search categories
        var categorySuggestions = await SearchCategoriesAsync(query, 3, cancellationToken);
        response.Suggestions.AddRange(categorySuggestions);

        // Add keyword suggestion
        if (response.Suggestions.Count < limit)
        {
            response.Suggestions.Insert(0, new SearchSuggestionDto
            {
                Type = "keyword",
                Text = query,
                SecondaryText = "Tìm kiếm",
                Url = $"/MultiSearch?keyword={Uri.EscapeDataString(query)}"
            });
        }

        // Add trending keywords if we have space
        if (response.Suggestions.Count < limit)
        {
            var trending = await GetTrendingSuggestionsAsync(limit - response.Suggestions.Count, cancellationToken);
            response.TrendingKeywords = trending;
        }

        return response;
    }

    public async Task<List<TrendingKeywordDto>> GetTrendingKeywordsAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);

        var trending = await _context.SearchHistories
            .Where(sh => sh.SearchTime >= cutoffDate && !string.IsNullOrEmpty(sh.DetectedKeyword))
            .GroupBy(sh => sh.DetectedKeyword.ToLower())
            .Select(g => new TrendingKeywordDto
            {
                Keyword = g.First().DetectedKeyword,
                SearchCount = g.Count(),
                LastSearched = g.Max(sh => sh.SearchTime) ?? DateTime.UtcNow
            })
            .OrderByDescending(t => t.SearchCount)
            .ThenByDescending(t => t.LastSearched)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Calculate trend score (recent searches weighted more)
        foreach (var item in trending)
        {
            var daysSinceSearch = (DateTime.UtcNow - item.LastSearched).TotalDays;
            item.TrendScore = item.SearchCount / (1 + daysSinceSearch * 0.1);
        }

        return trending.OrderByDescending(t => t.TrendScore).ToList();
    }

    public async Task RecordSearchAsync(
        string keyword, 
        int? userId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        try
        {
            var searchHistory = new SearchHistory
            {
                UserId = userId,
                SearchType = "Text",
                InputContent = keyword,
                DetectedKeyword = keyword,
                SearchTime = DateTime.UtcNow
            };

            _context.SearchHistories.Add(searchHistory);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record search for keyword: {Keyword}", keyword);
        }
    }

    #region Private Methods

    private async Task<List<SearchSuggestionDto>> SearchProductsAsync(
        string query, 
        int limit, 
        CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{query}%"))
            .Include(p => p.Platform)
            .OrderByDescending(p => p.LastUpdated)
            .Take(limit)
            .Select(p => new SearchSuggestionDto
            {
                Type = "product",
                Text = p.ProductName ?? "",
                SecondaryText = p.Platform != null ? p.Platform.PlatformName : null,
                ImageUrl = p.ImageUrl,
                Price = p.CurrentPrice,
                Platform = p.Platform != null ? p.Platform.PlatformName : null,
                Url = p.OriginalUrl
            })
            .ToListAsync(cancellationToken);

        return products;
    }

    private async Task<List<SearchSuggestionDto>> SearchCategoriesAsync(
        string query, 
        int limit, 
        CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .Where(c => EF.Functions.Like(c.CategoryName, $"%{query}%"))
            .Take(limit)
            .Select(c => new SearchSuggestionDto
            {
                Type = "category",
                Text = c.CategoryName,
                SecondaryText = "Danh mục",
                ImageUrl = c.IconUrl,
                Url = $"/Category/{c.CategoryId}"
            })
            .ToListAsync(cancellationToken);

        return categories;
    }

    private async Task<List<SearchSuggestionDto>> GetTrendingSuggestionsAsync(
        int limit, 
        CancellationToken cancellationToken)
    {
        var trending = await GetTrendingKeywordsAsync(limit, cancellationToken);

        return trending.Select(t => new SearchSuggestionDto
        {
            Type = "trending",
            Text = t.Keyword,
            SecondaryText = $"{t.SearchCount} lượt tìm kiếm",
            IsPopular = true,
            SearchCount = t.SearchCount,
            Url = $"/MultiSearch?keyword={Uri.EscapeDataString(t.Keyword)}"
        }).ToList();
    }

    private async Task<List<SearchSuggestionDto>> GetRecentSearchSuggestionsAsync(
        int userId, 
        int limit, 
        CancellationToken cancellationToken)
    {
        var recent = await _context.SearchHistories
            .Where(sh => sh.UserId == userId && !string.IsNullOrEmpty(sh.DetectedKeyword))
            .OrderByDescending(sh => sh.SearchTime)
            .Take(limit)
            .Select(sh => new SearchSuggestionDto
            {
                Type = "history",
                Text = sh.DetectedKeyword,
                SecondaryText = "Tìm kiếm gần đây",
                Url = $"/MultiSearch?keyword={Uri.EscapeDataString(sh.DetectedKeyword)}"
            })
            .ToListAsync(cancellationToken);

        return recent;
    }

    private bool IsUrl(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Check if it's a URL
        return text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("tiki.vn") ||
               text.Contains("shopee.vn") ||
               text.Contains("lazada.vn");
    }

    #endregion
}
