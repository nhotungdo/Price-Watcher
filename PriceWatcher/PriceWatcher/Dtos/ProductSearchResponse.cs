using System.Text.Json.Serialization;

namespace PriceWatcher.Dtos;

public record ProductSearchResponse
{
    public string Query { get; init; } = string.Empty;
    public string SearchMode { get; init; } = "keyword";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalItems { get; init; }
    public bool HasMore { get; init; }
    public long DurationMs { get; init; }
    public IReadOnlyList<ProductSearchItemDto> Items { get; init; } = Array.Empty<ProductSearchItemDto>();
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<SearchNotification> Notices { get; init; } = Array.Empty<SearchNotification>();

    [JsonIgnore]
    public IReadOnlyList<ProductCandidateDto> HistoryPayload { get; init; } = Array.Empty<ProductCandidateDto>();

    public static ProductSearchResponse Empty(string query = "", string mode = "keyword")
        => new()
        {
            Query = query,
            SearchMode = mode
        };
}

