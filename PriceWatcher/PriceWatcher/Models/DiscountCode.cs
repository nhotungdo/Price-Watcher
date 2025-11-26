namespace PriceWatcher.Models;

public class DiscountCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? PlatformId { get; set; }
    public int? ProductId { get; set; }
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "percentage"; // percentage, fixed_amount, free_shipping
    public decimal? DiscountValue { get; set; }
    public decimal? MinPurchase { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int SuccessCount { get; set; }
    public int TotalUses { get; set; }
    public int? SubmittedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastVerifiedAt { get; set; }

    // Navigation properties
    public Platform? Platform { get; set; }
    public Product? Product { get; set; }
    public User? SubmittedBy { get; set; }
}
