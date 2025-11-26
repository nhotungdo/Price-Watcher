namespace PriceWatcher.Dtos;

public class SearchFilters
{
    public string? Keyword { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<int> CategoryIds { get; set; } = new();
    public List<int> PlatformIds { get; set; } = new();
    public decimal? MinRating { get; set; }
    public string SortBy { get; set; } = "relevance"; // relevance, price_asc, price_desc, rating, popularity, newest
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public bool FreeShippingOnly { get; set; }
    public bool VerifiedStoresOnly { get; set; }
}

public class SearchResult
{
    public List<ProductSearchItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public SearchFilters AppliedFilters { get; set; } = new();
    public Dictionary<string, int> CategoryCounts { get; set; } = new();
    public Dictionary<string, int> PlatformCounts { get; set; } = new();
    public decimal? MinPriceFound { get; set; }
    public decimal? MaxPriceFound { get; set; }
}
