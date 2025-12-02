using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisualSearchController : ControllerBase
{
    private readonly IVisualSearchService _visualSearchService;
    private readonly ILogger<VisualSearchController> _logger;

    // Maximum file size: 10MB
    private const long MaxFileSize = 10 * 1024 * 1024;

    // Allowed image types
    private static readonly string[] AllowedContentTypes = 
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    public VisualSearchController(
        IVisualSearchService visualSearchService,
        ILogger<VisualSearchController> logger)
    {
        _visualSearchService = visualSearchService;
        _logger = logger;
    }

    /// <summary>
    /// Search for products by uploading an image file
    /// </summary>
    /// <param name="file">Image file (JPEG, PNG, WebP)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visual matches from Shopee, Lazada, and Tiki</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(VisualSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> SearchByUpload(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // Validate file
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { error = $"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB" });
        }

        if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new 
            { 
                error = "Invalid file type. Allowed types: JPEG, PNG, WebP",
                receivedType = file.ContentType
            });
        }

        _logger.LogInformation("Visual search request received. File: {FileName}, Size: {Size} bytes", 
            file.FileName, file.Length);

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _visualSearchService.SearchByImageAsync(stream, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation during visual search");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during visual search");
            return StatusCode(500, new { error = "An error occurred during visual search" });
        }
    }

    /// <summary>
    /// Search for products using an image URL
    /// </summary>
    /// <param name="request">Request containing image URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visual matches from Shopee, Lazada, and Tiki</returns>
    [HttpPost("url")]
    [ProducesResponseType(typeof(VisualSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchByUrl(
        [FromBody] ImageUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return BadRequest(new { error = "Image URL is required" });
        }

        if (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri))
        {
            return BadRequest(new { error = "Invalid image URL format" });
        }

        _logger.LogInformation("Visual search by URL request received: {Url}", request.ImageUrl);

        try
        {
            var result = await _visualSearchService.SearchByImageUrlAsync(request.ImageUrl, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation during visual search");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during visual search by URL");
            return StatusCode(500, new { error = "An error occurred during visual search" });
        }
    }

    /// <summary>
    /// Get information about the visual search service
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            service = "Visual Search using Google Lens (SerpApi)",
            supportedPlatforms = new[] { "Shopee", "Lazada", "Tiki" },
            maxFileSize = $"{MaxFileSize / 1024 / 1024}MB",
            allowedFormats = new[] { "JPEG", "PNG", "WebP" },
            endpoints = new
            {
                uploadImage = "POST /api/visualsearch/upload",
                searchByUrl = "POST /api/visualsearch/url",
                info = "GET /api/visualsearch/info"
            }
        });
    }
}

public record ImageUrlRequest(string ImageUrl);
