namespace PriceWatcher.Dtos;

/// <summary>
/// Result of tracking a product by URL
/// </summary>
public class ProductTrackingResultDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int? DiscountRate { get; set; }
    public string? ShopName { get; set; }
    public double? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public string? StockStatus { get; set; }
    public bool IsNewProduct { get; set; }
    public bool IsPriceUpdated { get; set; }
    public PriceHistoryChartDto PriceHistory { get; set; } = new();
}

/// <summary>
/// Product with complete price history
/// </summary>
public class ProductWithHistoryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int? DiscountRate { get; set; }
    public string? ShopName { get; set; }
    public double? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public int? SoldQuantity { get; set; }
    public string? StockStatus { get; set; }
    public DateTime? LastUpdated { get; set; }
    public PriceHistoryChartDto PriceHistory { get; set; } = new();
}

/// <summary>
/// Price history chart data
/// </summary>
public class PriceHistoryChartDto
{
    public List<PricePointDto> DataPoints { get; set; } = new();
    public decimal? LowestPrice { get; set; }
    public decimal? HighestPrice { get; set; }
    public decimal? AveragePrice { get; set; }
    public decimal? PriceChange { get; set; }
    public double? PriceChangePercent { get; set; }
    public int TotalSnapshots { get; set; }
}

/// <summary>
/// Individual price point for chart
/// </summary>
public class PricePointDto
{
    public DateTime RecordedAt { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
}
