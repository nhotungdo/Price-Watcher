namespace PriceWatcher.Dtos;

public class SuggestedProductDto
{
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int? DiscountRate { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public int PlatformId { get; set; }
    public string PlatformLogo { get; set; } = string.Empty;
    public string PlatformColor { get; set; } = string.Empty;
    public double? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public string? ShopName { get; set; }
    public bool IsFreeShip { get; set; }
    public DateTime LastUpdated { get; set; }
}
