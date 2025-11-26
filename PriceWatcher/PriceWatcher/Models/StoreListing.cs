namespace PriceWatcher.Models;

public class StoreListing
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int PlatformId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public decimal? StoreRating { get; set; }
    public bool IsVerified { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsOfficial { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? ShippingCost { get; set; }
    public int? DeliveryDays { get; set; }
    public int? Stock { get; set; }
    public bool IsFreeShipping { get; set; }
    public string? StoreUrl { get; set; }
    public int? TotalSales { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Product? Product { get; set; }
    public Platform? Platform { get; set; }
}
