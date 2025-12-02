namespace PriceWatcher.Dtos;

/// <summary>
/// Request for multi-platform product search
/// </summary>
public class MultiPlatformSearchRequest
{
    public string Keyword { get; set; } = string.Empty;
    public List<string>? Platforms { get; set; } // null = all platforms
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
    public MultiPlatformSearchFilters? Filters { get; set; }
    public string? SortBy { get; set; } // "price_asc", "price_desc", "rating", "sold"
}

/// <summary>
/// Search filters for multi-platform product search
/// </summary>
public class MultiPlatformSearchFilters
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinRating { get; set; }
    public bool? FreeShipping { get; set; }
    public bool? OfficialStore { get; set; }
}

/// <summary>
/// Response containing products from multiple platforms
/// </summary>
public class MultiPlatformSearchResponse
{
    public string Keyword { get; set; } = string.Empty;
    public int TotalResults { get; set; }
    public List<PlatformProductDto> Products { get; set; } = new();
    public Dictionary<string, int> ResultsByPlatform { get; set; } = new();
    public SearchMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Product from a specific platform with all details
/// </summary>
public class PlatformProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? PriceBeforeDiscount { get; set; }
    public double? DiscountPercent { get; set; }
    public double? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public int? SoldCount { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string PlatformLogo { get; set; } = string.Empty;
    public bool IsFreeShip { get; set; }
    public bool IsOfficialStore { get; set; }
    public string? SellerType { get; set; }
    public decimal ShippingCost { get; set; }
    public bool IsOutOfStock { get; set; }
}

/// <summary>
/// Metadata about the search operation
/// </summary>
public class SearchMetadata
{
    public long SearchDurationMs { get; set; }
    public Dictionary<string, long> PlatformDurations { get; set; } = new();
    public Dictionary<string, string?> PlatformErrors { get; set; } = new();
    public DateTime SearchTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Price comparison for similar products
/// </summary>
public class PriceComparisonDto
{
    public string ProductName { get; set; } = string.Empty;
    public List<PlatformPriceDto> Prices { get; set; } = new();
    public decimal LowestPrice { get; set; }
    public decimal HighestPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public string BestDealPlatform { get; set; } = string.Empty;
}

/// <summary>
/// Price information for a specific platform
/// </summary>
public class PlatformPriceDto
{
    public string Platform { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalCost { get; set; }
    public double? Rating { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
}
