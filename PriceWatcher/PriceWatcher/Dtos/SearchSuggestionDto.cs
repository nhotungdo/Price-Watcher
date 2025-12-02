namespace PriceWatcher.Dtos;

public class SearchSuggestionDto
{
    public string Type { get; set; } = string.Empty; // "product", "keyword", "category", "history", "trending"
    public string Text { get; set; } = string.Empty;
    public string? SecondaryText { get; set; }
    public string? ImageUrl { get; set; }
    public string? Url { get; set; }
    public decimal? Price { get; set; }
    public string? Platform { get; set; }
    public int? SearchCount { get; set; }
    public bool IsPopular { get; set; }
}

public class SearchSuggestionsResponse
{
    public bool Success { get; set; }
    public List<SearchSuggestionDto> Suggestions { get; set; } = new();
    public List<SearchSuggestionDto> TrendingKeywords { get; set; } = new();
    public List<SearchSuggestionDto> RecentSearches { get; set; } = new();
    public string? DetectedType { get; set; } // "url", "text", "empty"
}

public class TrendingKeywordDto
{
    public string Keyword { get; set; } = string.Empty;
    public int SearchCount { get; set; }
    public double TrendScore { get; set; }
    public DateTime LastSearched { get; set; }
}
