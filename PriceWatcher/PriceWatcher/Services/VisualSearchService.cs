using Microsoft.Extensions.Options;
using PriceWatcher.Dtos;
using PriceWatcher.Options;
using PriceWatcher.Services.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PriceWatcher.Services;

/// <summary>
/// Visual search service using SerpApi's Google Lens API
/// </summary>
public class VisualSearchService : IVisualSearchService
{
    private readonly HttpClient _httpClient;
    private readonly SerpApiOptions _options;
    private readonly ILogger<VisualSearchService> _logger;
    private readonly IMetricsService _metrics;

    // Supported e-commerce platforms
    private static readonly string[] SupportedPlatforms = { "shopee.vn", "tiki.vn" };

    public VisualSearchService(
        IHttpClientFactory httpClientFactory,
        IOptions<SerpApiOptions> options,
        ILogger<VisualSearchService> logger,
        IMetricsService metrics)
    {
        _httpClient = httpClientFactory.CreateClient("serpapi");
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<VisualSearchResponseDto> SearchByImageAsync(
        Stream imageStream, 
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("SerpApi is disabled in configuration");
            return new VisualSearchResponseDto();
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("SerpApi API key is not configured");
            throw new InvalidOperationException("SerpApi API key is required");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Upload image to SerpApi
            var imageUrl = await UploadImageAsync(imageStream, cancellationToken);
            
            // Search using the uploaded image URL
            var result = await SearchByImageUrlAsync(imageUrl, cancellationToken);

            _metrics.RecordScraperCall("serpapi", success: true, elapsedMs: sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during visual search with uploaded image");
            _metrics.RecordScraperCall("serpapi", success: false, elapsedMs: sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<VisualSearchResponseDto> SearchByImageUrlAsync(
        string imageUrl, 
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("SerpApi is disabled in configuration");
            return new VisualSearchResponseDto();
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("SerpApi API key is not configured");
            throw new InvalidOperationException("SerpApi API key is required");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Performing visual search with image URL: {ImageUrl}", imageUrl);

            // Build SerpApi Google Lens request
            var requestUrl = BuildGoogleLensUrl(imageUrl);

            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = ParseSerpApiResponse(jsonContent, imageUrl);

            _logger.LogInformation("Visual search completed. Found {Count} results from supported platforms", 
                result.TotalResults);

            _metrics.RecordScraperCall("serpapi", success: true, elapsedMs: sw.ElapsedMilliseconds);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during SerpApi request");
            _metrics.RecordScraperCall("serpapi", success: false, elapsedMs: sw.ElapsedMilliseconds);
            throw new InvalidOperationException("Failed to communicate with SerpApi", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during visual search");
            _metrics.RecordScraperCall("serpapi", success: false, elapsedMs: sw.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<string> UploadImageAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        try
        {
            // Convert stream to byte array
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, cancellationToken);
            var imageBytes = memoryStream.ToArray();

            // Create multipart form data
            using var content = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "file", "upload.jpg");

            // Upload to SerpApi
            var uploadUrl = $"{_options.BaseUrl}/searches/{_options.ApiKey}/upload";
            var response = await _httpClient.PostAsync(uploadUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(jsonResponse);

            if (doc.RootElement.TryGetProperty("image_url", out var imageUrlProp))
            {
                var uploadedUrl = imageUrlProp.GetString();
                if (!string.IsNullOrWhiteSpace(uploadedUrl))
                {
                    _logger.LogInformation("Image uploaded successfully: {Url}", uploadedUrl);
                    return uploadedUrl;
                }
            }

            throw new InvalidOperationException("Failed to get uploaded image URL from SerpApi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to SerpApi");
            throw new InvalidOperationException("Failed to upload image", ex);
        }
    }

    private string BuildGoogleLensUrl(string imageUrl)
    {
        var encodedImageUrl = Uri.EscapeDataString(imageUrl);
        return $"{_options.BaseUrl}/search.json?engine=google_lens&url={encodedImageUrl}&api_key={_options.ApiKey}&hl=vi&gl=vn";
    }

    private VisualSearchResponseDto ParseSerpApiResponse(string jsonContent, string imageUrl)
    {
        var response = new VisualSearchResponseDto
        {
            ImageUrl = imageUrl,
            Results = new List<VisualSearchResultDto>()
        };

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Get search ID
            if (root.TryGetProperty("search_metadata", out var metadata))
            {
                if (metadata.TryGetProperty("id", out var searchId))
                {
                    response.SearchId = searchId.GetString();
                }
            }

            // Parse visual matches
            if (root.TryGetProperty("visual_matches", out var visualMatches) && 
                visualMatches.ValueKind == JsonValueKind.Array)
            {
                foreach (var match in visualMatches.EnumerateArray())
                {
                    var result = ParseVisualMatch(match);
                    if (result != null && IsSupportedPlatform(result.SourceUrl))
                    {
                        response.Results.Add(result);
                    }
                }
            }

            // Also check shopping results if available
            if (root.TryGetProperty("shopping_results", out var shoppingResults) && 
                shoppingResults.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in shoppingResults.EnumerateArray())
                {
                    var result = ParseShoppingResult(item);
                    if (result != null && IsSupportedPlatform(result.SourceUrl))
                    {
                        response.Results.Add(result);
                    }
                }
            }

            response.TotalResults = response.Results.Count;

            _logger.LogInformation("Parsed {Total} results from SerpApi response", response.TotalResults);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing SerpApi JSON response");
            throw new InvalidOperationException("Failed to parse SerpApi response", ex);
        }

        return response;
    }

    private VisualSearchResultDto? ParseVisualMatch(JsonElement match)
    {
        try
        {
            var result = new VisualSearchResultDto();

            // Title
            if (match.TryGetProperty("title", out var title))
            {
                result.Title = title.GetString() ?? string.Empty;
            }

            // Source URL
            if (match.TryGetProperty("link", out var link))
            {
                result.SourceUrl = link.GetString() ?? string.Empty;
            }

            // Thumbnail
            if (match.TryGetProperty("thumbnail", out var thumbnail))
            {
                result.ThumbnailUrl = thumbnail.GetString();
            }

            // Source/Domain
            if (match.TryGetProperty("source", out var source))
            {
                result.Source = source.GetString();
            }

            // Price (if available)
            if (match.TryGetProperty("price", out var price))
            {
                var priceObj = price;
                if (priceObj.TryGetProperty("value", out var priceValue))
                {
                    result.Price = priceValue.GetString();
                    result.PriceValue = ParsePrice(result.Price);
                }
                if (priceObj.TryGetProperty("currency", out var currency))
                {
                    result.Currency = currency.GetString();
                }
            }

            // Determine platform
            result.Platform = DeterminePlatform(result.SourceUrl);

            if (string.IsNullOrWhiteSpace(result.Title) || string.IsNullOrWhiteSpace(result.SourceUrl))
            {
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing visual match");
            return null;
        }
    }

    private VisualSearchResultDto? ParseShoppingResult(JsonElement item)
    {
        try
        {
            var result = new VisualSearchResultDto();

            // Title
            if (item.TryGetProperty("title", out var title))
            {
                result.Title = title.GetString() ?? string.Empty;
            }

            // Link
            if (item.TryGetProperty("link", out var link))
            {
                result.SourceUrl = link.GetString() ?? string.Empty;
            }

            // Price
            if (item.TryGetProperty("price", out var price))
            {
                result.Price = price.GetString();
                result.PriceValue = ParsePrice(result.Price);
            }

            // Source
            if (item.TryGetProperty("source", out var source))
            {
                result.Source = source.GetString();
            }

            // Thumbnail
            if (item.TryGetProperty("thumbnail", out var thumbnail))
            {
                result.ThumbnailUrl = thumbnail.GetString();
            }

            // Platform
            result.Platform = DeterminePlatform(result.SourceUrl);

            if (string.IsNullOrWhiteSpace(result.Title) || string.IsNullOrWhiteSpace(result.SourceUrl))
            {
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing shopping result");
            return null;
        }
    }

    private bool IsSupportedPlatform(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return SupportedPlatforms.Any(platform => 
            url.Contains(platform, StringComparison.OrdinalIgnoreCase));
    }

    private string DeterminePlatform(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "unknown";

        if (url.Contains("shopee.vn", StringComparison.OrdinalIgnoreCase))
            return "shopee";
        if (url.Contains("tiki.vn", StringComparison.OrdinalIgnoreCase))
            return "tiki";

        return "unknown";
    }

    private decimal? ParsePrice(string? priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText))
            return null;

        try
        {
            // Remove currency symbols and clean up
            var cleaned = Regex.Replace(priceText, @"[₫đ,.\s]", "");
            cleaned = Regex.Replace(cleaned, @"[^\d]", "");

            if (decimal.TryParse(cleaned, out var price))
            {
                return price;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing price: {Price}", priceText);
        }

        return null;
    }
}
