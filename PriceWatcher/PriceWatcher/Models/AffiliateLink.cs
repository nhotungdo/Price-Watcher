namespace PriceWatcher.Models;

public class AffiliateLink
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int PlatformId { get; set; }
    public string AffiliateUrl { get; set; } = string.Empty;
    public string? AffiliateCode { get; set; }
    public int ClickCount { get; set; }
    public int ConversionCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal? CommissionRate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastClickedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Product? Product { get; set; }
    public Platform? Platform { get; set; }
}
