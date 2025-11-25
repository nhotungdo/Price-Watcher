using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class ProductSearchService : IProductSearchService
{
    private const int MaxCandidates = 800;
    private const decimal ScoreThreshold = 0.18m;
    private static readonly Regex CollapseSpacesRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILinkProcessor _linkProcessor;
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<ProductSearchService> _logger;

    public ProductSearchService(
        PriceWatcherDbContext dbContext,
        ILinkProcessor linkProcessor,
        IRecommendationService recommendationService,
        ILogger<ProductSearchService> logger)
    {
        _dbContext = dbContext;
        _linkProcessor = linkProcessor;
        _recommendationService = recommendationService;
        _logger = logger;
    }

    public async Task<ProductSearchResponse> SearchAsync(string input, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var sanitizedPage = Math.Max(1, page);
        var sanitizedPageSize = Math.Clamp(pageSize, 5, 48);

        if (string.IsNullOrWhiteSpace(input))
        {
            return ProductSearchResponse.Empty();
        }

        var trimmed = input.Trim();
        var mode = LooksLikeUrl(trimmed) ? "url" : "keyword";
        var sw = Stopwatch.StartNew();

        ProductSearchResponse response = mode == "url"
            ? await SearchByUrlAsync(trimmed, sanitizedPage, sanitizedPageSize, cancellationToken)
            : await SearchByKeywordAsync(trimmed, sanitizedPage, sanitizedPageSize, cancellationToken);

        sw.Stop();
        return response with { DurationMs = sw.ElapsedMilliseconds };
    }

    public async Task<IReadOnlyList<(int? productId, string name)>> SuggestAsync(string keyword, int limit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Array.Empty<(int?, string)>();
        }

        var normalized = Normalize(keyword);
        var baseToken = Tokenize(normalized).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(baseToken))
        {
            return Array.Empty<(int?, string)>();
        }

        var query = _dbContext.Products.AsNoTracking()
            .Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName!, $"%{baseToken}%"))
            .OrderByDescending(p => p.LastUpdated ?? DateTime.MinValue)
            .Take(Math.Clamp(limit * 4, 8, 40));

        var projections = await query
            .Select(p => new { p.ProductId, p.ProductName })
            .ToListAsync(cancellationToken);

        var tokens = Tokenize(normalized);
        var results = projections
            .Select(p =>
            {
                var name = p.ProductName ?? string.Empty;
                var score = ComputeScore(tokens, Normalize(name));
                return new { p.ProductId, Name = name, Score = score };
            })
            .Where(x => x.Score >= ScoreThreshold)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => ((int?)x.ProductId, x.Name))
            .ToList();

        return results;
    }

    private async Task<ProductSearchResponse> SearchByKeywordAsync(string keyword, int page, int pageSize, CancellationToken cancellationToken)
    {
        var normalized = Normalize(keyword);
        var tokens = Tokenize(normalized);
        if (tokens.Length == 0)
        {
            return ProductSearchResponse.Empty(keyword);
        }

        var baseToken = tokens[0];

        var query = _dbContext.Products.AsNoTracking()
            .Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName!, $"%{baseToken}%"))
            .OrderByDescending(p => p.LastUpdated ?? DateTime.MinValue)
            .Take(MaxCandidates);

        var projections = await query
            .Select(p => new ProductSearchProjection
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName!,
                ShopName = p.ShopName,
                PlatformName = p.Platform != null ? p.Platform.PlatformName : null,
                ImageUrl = p.ImageUrl,
                ProductUrl = p.OriginalUrl,
                Rating = p.Rating,
                ReviewCount = p.ReviewCount,
                Price = p.CurrentPrice,
                LastUpdated = p.LastUpdated
            })
            .ToListAsync(cancellationToken);

        var rawTokens = TokenizeRaw(keyword);
        var scored = projections
            .Select(p =>
            {
                var normalizedName = Normalize(p.ProductName);
                var score = ComputeScore(tokens, normalizedName);
                return new { Projection = p, Score = score };
            })
            .Where(x => x.Score >= ScoreThreshold)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Projection.LastUpdated ?? DateTime.MinValue)
            .ToList();

        var total = scored.Count;
        var paged = scored
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x.Projection, x.Score, rawTokens, keyword))
            .ToList();

        var historyCandidates = paged
            .Select(ToCandidateDto)
            .ToList();

        var suggestions = scored
            .Take(6)
            .Select(x => x.Projection.ProductName)
            .Distinct()
            .ToList();

        var notices = total == 0
            ? new[] { new SearchNotification("info", "Không tìm thấy sản phẩm phù hợp với từ khóa đã nhập.") }
            : Array.Empty<SearchNotification>();

        return new ProductSearchResponse
        {
            Query = keyword,
            SearchMode = "keyword",
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            HasMore = total > page * pageSize,
            Items = paged,
            Suggestions = suggestions,
            Notices = notices,
            HistoryPayload = historyCandidates
        };
    }

    private async Task<ProductSearchResponse> SearchByUrlAsync(string url, int page, int pageSize, CancellationToken cancellationToken)
    {
        var notices = new List<SearchNotification>();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            notices.Add(new SearchNotification("error", "Định dạng URL không hợp lệ."));
            return ProductSearchResponse.Empty(url, "url") with { Notices = notices };
        }

        if (!IsSupportedHost(uri.Host))
        {
            notices.Add(new SearchNotification("error", "URL không thuộc các nền tảng được hỗ trợ (Shopee, Lazada, Tiki)."));
            return ProductSearchResponse.Empty(url, "url") with { Notices = notices };
        }

        ProductQuery query;
        try
        {
            query = await _linkProcessor.ProcessUrlAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "URL processing failed for {Url}", url);
            notices.Add(new SearchNotification("error", "Không thể phân tích URL sản phẩm này."));
            return ProductSearchResponse.Empty(url, "url") with { Notices = notices };
        }

        var canonical = query.CanonicalUrl ?? url;

        var matches = await _dbContext.Products.AsNoTracking()
            .Where(p => p.OriginalUrl == canonical || (query.ProductId != null && (p.ExternalId == query.ProductId || p.OriginalUrl.Contains(query.ProductId))))
            .OrderByDescending(p => p.LastUpdated ?? DateTime.MinValue)
            .Take(5)
            .Select(p => new ProductSearchProjection
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName!,
                ShopName = p.ShopName,
                PlatformName = p.Platform != null ? p.Platform.PlatformName : null,
                ImageUrl = p.ImageUrl,
                ProductUrl = p.OriginalUrl,
                Rating = p.Rating,
                ReviewCount = p.ReviewCount,
                Price = p.CurrentPrice,
                LastUpdated = p.LastUpdated
            })
            .ToListAsync(cancellationToken);

        List<ProductSearchItemDto> items;
        List<ProductCandidateDto> history;

        if (matches.Count > 0)
        {
            items = matches
                .Select(p => MapToDto(p, 1m, Array.Empty<string>(), p.ProductName))
                .ToList();
            history = items.Select(ToCandidateDto).ToList();
        }
        else
        {
            notices.Add(new SearchNotification("warning", "Chưa tìm thấy sản phẩm trong kho dữ liệu. Đang thử tìm kiếm trực tiếp..."));

            var recommendations = await TryRecommendAsync(query, cancellationToken);

            if (recommendations.Count == 0)
            {
                notices.Add(new SearchNotification("error", "Không thể tìm thấy sản phẩm tương ứng với URL đã cung cấp."));
                return new ProductSearchResponse
                {
                    Query = url,
                    SearchMode = "url",
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = 0,
                    Items = Array.Empty<ProductSearchItemDto>(),
                    Suggestions = Array.Empty<string>(),
                    Notices = notices,
                    HistoryPayload = Array.Empty<ProductCandidateDto>()
                };
            }

            items = recommendations
                .Select(r => new ProductSearchItemDto
                {
                    ProductId = r.ProductId,
                    Title = r.Title,
                    HighlightedTitle = r.Title,
                    Platform = r.Platform,
                    ShopName = r.ShopName,
                    ImageUrl = r.ThumbnailUrl,
                    ProductUrl = r.ProductUrl,
                    Price = r.Price,
                    Rating = r.ShopRating,
                    ReviewCount = r.SoldCount,
                    MatchScore = r.MatchScore,
                    Labels = r.Labels.ToArray(),
                    IsExactMatch = true
                })
                .ToList();
            history = recommendations.ToList();
            notices.Add(new SearchNotification("info", "Đã trả về kết quả trực tiếp từ các nền tảng."));
        }

        return new ProductSearchResponse
        {
            Query = url,
            SearchMode = "url",
            Page = 1,
            PageSize = items.Count,
            TotalItems = items.Count,
            Items = items,
            Suggestions = Array.Empty<string>(),
            Notices = notices,
            HistoryPayload = history
        };
    }

    private async Task<IReadOnlyList<ProductCandidateDto>> TryRecommendAsync(ProductQuery query, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(450));
            var res = await _recommendationService.RecommendAsync(query, top: 8, timeoutCts.Token);
            return res.ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Recommendation timed out for {@Query}", query);
            return Array.Empty<ProductCandidateDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recommendation failed for {@Query}", query);
            return Array.Empty<ProductCandidateDto>();
        }
    }

    private static ProductSearchItemDto MapToDto(ProductSearchProjection projection, decimal score, string[] rawTokens, string keyword)
    {
        return new ProductSearchItemDto
        {
            ProductId = projection.ProductId,
            Title = projection.ProductName,
            HighlightedTitle = BuildHighlight(projection.ProductName, rawTokens),
            Platform = projection.PlatformName,
            ShopName = projection.ShopName,
            ImageUrl = projection.ImageUrl,
            ProductUrl = projection.ProductUrl,
            Price = projection.Price,
            Rating = projection.Rating,
            ReviewCount = projection.ReviewCount,
            MatchScore = score,
            IsExactMatch = string.Equals(Normalize(projection.ProductName), Normalize(keyword), StringComparison.Ordinal)
        };
    }

    private static ProductCandidateDto ToCandidateDto(ProductSearchItemDto item)
    {
        return new ProductCandidateDto
        {
            ProductId = item.ProductId,
            Platform = item.Platform ?? string.Empty,
            Title = item.Title,
            Price = item.Price ?? 0,
            ShippingCost = 0,
            ShopName = item.ShopName ?? string.Empty,
            ShopRating = item.Rating ?? 0,
            ShopSales = item.ReviewCount ?? 0,
            ProductUrl = item.ProductUrl ?? string.Empty,
            ThumbnailUrl = item.ImageUrl ?? string.Empty,
            MatchScore = item.MatchScore,
            Labels = item.Labels
        };
    }

    private static bool LooksLikeUrl(string input)
        => Uri.TryCreate(input, UriKind.Absolute, out _);

    private static bool IsSupportedHost(string host)
    {
        var lowered = host.ToLowerInvariant();
        return lowered.Contains("shopee") || lowered.Contains("lazada") || lowered.Contains("tiki");
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

    private static string[] TokenizeRaw(string text)
        => string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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

    private static string BuildHighlight(string source, string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(source) || tokens.Length == 0)
        {
            return source;
        }

        var highlighted = source;
        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token)) continue;
            highlighted = Regex.Replace(highlighted, Regex.Escape(token), match => $"<mark>{match.Value}</mark>", RegexOptions.IgnoreCase);
        }
        return highlighted;
    }

    private sealed class ProductSearchProjection
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ShopName { get; set; }
        public string? PlatformName { get; set; }
        public string? ImageUrl { get; set; }
        public string? ProductUrl { get; set; }
        public double? Rating { get; set; }
        public int? ReviewCount { get; set; }
        public decimal? Price { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}

