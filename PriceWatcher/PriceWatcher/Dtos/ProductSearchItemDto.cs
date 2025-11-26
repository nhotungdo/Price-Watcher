namespace PriceWatcher.Dtos;

public class ProductSearchItemDto
{
    public int? ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string HighlightedTitle { get; set; } = string.Empty;
    public string? Platform { get; set; }
    public string? ShopName { get; set; }
    public decimal? Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string Currency { get; set; } = "VND";
    public double? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public decimal MatchScore { get; set; }
    public bool IsExactMatch { get; set; }
    public string[] Labels { get; set; } = Array.Empty<string>();
    public bool IsFreeShip { get; set; }
    public bool IsVerified { get; set; }
    public int DiscountPercent { get; set; }
}

