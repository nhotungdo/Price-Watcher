namespace PriceWatcher.Dtos;

public class ProductCandidateDto
{
    public string Platform { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalCost => Price + ShippingCost;
    public string ShopName { get; set; } = string.Empty;
    public double ShopRating { get; set; }
    public int ShopSales { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Labels { get; set; } = Array.Empty<string>();
    public decimal MatchScore { get; set; }
    public string? FitReason { get; set; }
    public double? ImageSimilarity { get; set; }
    public bool? IsImageMatch { get; set; }
    public decimal? OriginalPrice { get; set; }
    public double? DiscountPercent { get; set; }
    public int? SoldCount { get; set; }
    public string? SellerType { get; set; }
}

