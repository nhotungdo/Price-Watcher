using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using PriceWatcher.Services;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("search")]
public class SearchController : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png" , "image/webp"};

    private readonly ISearchJobQueue _jobQueue;
    private readonly ISearchStatusService _statusService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchJobQueue jobQueue, ISearchStatusService statusService, ILogger<SearchController> logger)
    {
        _jobQueue = jobQueue;
        _statusService = statusService;
        _logger = logger;
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
            return BadRequest("url is required for JSON submissions.");
        }

        if (!IsSupportedProductUrl(request.Url))
        {
            return BadRequest("URL sản phẩm không hợp lệ hoặc không thuộc nền tảng hỗ trợ.");
        }

        return await QueueSearchAsync(request.UserId, "url", request.Url, null, cancellationToken, null, null);
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

        if (!string.IsNullOrWhiteSpace(request.Url) && !IsSupportedProductUrl(request.Url))
        {
            return BadRequest("URL sản phẩm không hợp lệ hoặc không thuộc nền tảng hỗ trợ.");
        }

        return await QueueSearchAsync(request.UserId, "url", request.Url!, null, cancellationToken, null, null);
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
}

