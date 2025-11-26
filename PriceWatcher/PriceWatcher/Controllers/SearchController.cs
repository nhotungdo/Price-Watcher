using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;
using PriceWatcher.Services;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("search")]
public class SearchController : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };

    private readonly ISearchJobQueue _jobQueue;
    private readonly ISearchStatusService _statusService;
    private readonly ILogger<SearchController> _logger;
    private readonly PriceWatcherDbContext _dbContext;
    private readonly IRecommendationService _recommendationService;
    private readonly IProductSearchService _productSearchService;
    private readonly ISearchHistoryService _searchHistoryService;

    public SearchController(
        ISearchJobQueue jobQueue,
        ISearchStatusService statusService,
        ILogger<SearchController> logger,
        PriceWatcherDbContext dbContext,
        IRecommendationService recommendationService,
        IProductSearchService productSearchService,
        ISearchHistoryService searchHistoryService)
    {
        _jobQueue = jobQueue;
        _statusService = statusService;
        _logger = logger;
        _dbContext = dbContext;
        _recommendationService = recommendationService;
        _productSearchService = productSearchService;
        _searchHistoryService = searchHistoryService;
    }


    [HttpPost("submit")]
    [Consumes("application/json")]
    public async Task<IActionResult> SubmitJson([FromBody] SearchSubmitRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest("Input is required.");
        }

        string searchType = "url";
        if (Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            if (!IsSupportedProductUrl(request.Url))
            {
                return BadRequest("URL sản phẩm không hợp lệ hoặc không thuộc nền tảng hỗ trợ (Shopee/Lazada/Tiki).");
            }
        }
        else
        {
            searchType = "keyword";
        }

        return await QueueSearchAsync(request.UserId, searchType, request.Url, null, cancellationToken, null, null);
    }

    [HttpPost("submit")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitForm([FromForm] SearchSubmitForm request, CancellationToken cancellationToken)
    {
        if (request.Image == null && string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest("Either url or image must be provided.");
        }

        if (request.Image != null)
        {
            if (!AllowedContentTypes.Contains(request.Image.ContentType))
            {
                return BadRequest("Only jpg/png/webp supported.");
            }

            if (request.Image.Length > MaxImageBytes)
            {
                return BadRequest("Image exceeds 5MB limit.");
            }

            await using var ms = new MemoryStream();
            await request.Image.CopyToAsync(ms, cancellationToken);
            var hint = GenerateKeywordFromFilename(request.Image.FileName);
            var queryOverride = string.IsNullOrWhiteSpace(hint) ? null : new ProductQuery { TitleHint = hint };
            return await QueueSearchAsync(request.UserId, "image", request.Url, ms.ToArray(), cancellationToken, request.Image.ContentType, queryOverride);
        }

        string searchType = "url";
        if (!string.IsNullOrWhiteSpace(request.Url))
        {
            if (Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            {
                if (!IsSupportedProductUrl(request.Url))
                {
                    return BadRequest("URL sản phẩm không hợp lệ hoặc không thuộc nền tảng hỗ trợ.");
                }
            }
            else
            {
                searchType = "keyword";
            }
        }

        return await QueueSearchAsync(request.UserId, searchType, request.Url!, null, cancellationToken, null, null);
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] int limit = 8, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(Array.Empty<object>());
        }
        var suggestions = await _productSearchService.SuggestAsync(q, limit, cancellationToken);
        if (suggestions.Count == 0)
        {
            _logger.LogInformation("Autocomplete no result for {Query}", q);
        }
        return Ok(suggestions.Select(x => new { id = x.productId, name = x.name }));
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string q, [FromQuery] int top = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Query is required");
        }
        ProductQuery query;
        if (Uri.TryCreate(q, UriKind.Absolute, out var uri) && IsSupportedProductUrl(q))
        {
            var platform = uri.Host.Contains("shopee", StringComparison.OrdinalIgnoreCase) ? "shopee"
                : uri.Host.Contains("lazada", StringComparison.OrdinalIgnoreCase) ? "lazada"
                : uri.Host.Contains("tiki", StringComparison.OrdinalIgnoreCase) ? "tiki" : null;
            query = new ProductQuery { CanonicalUrl = q, Platform = platform };
        }
        else
        {
            query = new ProductQuery { TitleHint = q };
        }
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(450));
        var res = await _recommendationService.RecommendAsync(query, top, cts.Token);
        if (!res.Any())
        {
            _logger.LogWarning("Preview no candidates for {Query}", q);
        }
        return Ok(res);
    }

    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Query is required.");
        }

        var response = await _productSearchService.SearchAsync(q, page, pageSize, cancellationToken);
        await PersistHistoryAsync(response, cancellationToken);
        return Ok(response);
    }

    [HttpGet("status/{searchId:guid}")]
    public IActionResult GetStatus(Guid searchId)
    {
        var status = _statusService.GetStatus(searchId);
        if (status == null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    private async Task<IActionResult> QueueSearchAsync(int userId, string searchType, string? url, byte[]? imageBytes, CancellationToken cancellationToken, string? imageContentType, ProductQuery? queryOverride)
    {
        var searchId = Guid.NewGuid();
        _statusService.Initialize(searchId);

        var job = new SearchJob
        {
            SearchId = searchId,
            UserId = userId,
            Url = url,
            ImageBytes = imageBytes,
            ImageContentType = imageContentType,
            SearchType = searchType,
            QueryOverride = queryOverride
        };

        await _jobQueue.QueueAsync(job, cancellationToken);
        _logger.LogInformation("Queued search {SearchId} for user {UserId}", searchId, userId);

        return Accepted(new { searchId });
    }

    private static bool IsSupportedProductUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }
        var host = uri.Host.ToLowerInvariant();
        if (!(host.Contains("shopee") || host.Contains("lazada") || host.Contains("tiki")))
        {
            return false;
        }
        return true;
    }

    private static string GenerateKeywordFromFilename(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty).ToLowerInvariant();
        name = RemoveDiacritics(name);
        name = System.Text.RegularExpressions.Regex.Replace(name, @"[_\-]+", " ");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9\p{L}\s]", "");
        var tokens = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var stop = new HashSet<string>(new[] { "img", "image", "anh", "hinh", "picture", "photo", "scan", "untitled", "new", "copy" });
        var filtered = tokens.Where(t => t.Length >= 2 && !stop.Contains(t) && !int.TryParse(t, out _)).Take(6);
        return string.Join(' ', filtered);
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static decimal ComputeScore(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;
        var ta = a.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToArray();
        var tb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToArray();
        var inter = ta.Intersect(tb).Count();
        var union = ta.Union(tb).Count();
        var jaccard = union == 0 ? 0 : (decimal)inter / union;
        var prefix = b.StartsWith(a, StringComparison.Ordinal) ? 0.3m : 0m;
        var contains = b.Contains(a, StringComparison.Ordinal) ? 0.2m : 0m;
        return Math.Min(1m, jaccard + prefix + contains);
    }

    private async Task PersistHistoryAsync(ProductSearchResponse response, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        if (userId == null || response.HistoryPayload.Count == 0)
        {
            return;
        }

        try
        {
            var query = response.SearchMode == "url"
                ? new ProductQuery { CanonicalUrl = response.Items.FirstOrDefault()?.ProductUrl ?? response.Query, TitleHint = response.Query }
                : new ProductQuery { TitleHint = response.Query };

            await _searchHistoryService.SaveSearchHistoryAsync(
                Guid.NewGuid(),
                userId.Value,
                response.SearchMode,
                response.Query,
                query,
                response.HistoryPayload,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist search history");
        }
    }

    private int? ResolveUserId()
    {
        var userIdClaim = User?.FindFirst("uid")?.Value ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
