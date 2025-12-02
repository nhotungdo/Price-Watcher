namespace PriceWatcher.Dtos;

/// <summary>
/// Represents a visual search result from Google Lens via SerpApi
/// </summary>
public class VisualSearchResultDto
{
    public string Title { get; set; } = string.Empty;
    public string? Price { get; set; }
    public decimal? PriceValue { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? Source { get; set; }
    public double? Similarity { get; set; }
    public string? Currency { get; set; }
}

/// <summary>
/// Response from visual search containing all matches
/// </summary>
public class VisualSearchResponseDto
{
    public List<VisualSearchResultDto> Results { get; set; } = new();
    public int TotalResults { get; set; }
    public string? SearchId { get; set; }
    public string? ImageUrl { get; set; }
}
