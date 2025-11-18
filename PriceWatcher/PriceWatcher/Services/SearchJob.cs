using PriceWatcher.Dtos;

namespace PriceWatcher.Services;

public class SearchJob
{
    public Guid SearchId { get; init; }
    public int UserId { get; init; }
    public string? Url { get; init; }
    public byte[]? ImageBytes { get; init; }
    public string? ImageContentType { get; init; }
    public string SearchType { get; init; } = "url";
    public ProductQuery? QueryOverride { get; init; }
}

