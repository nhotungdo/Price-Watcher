namespace PriceWatcher.Models;

public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PlatformId { get; set; }
    public decimal? Rating { get; set; }
    public bool IsVerified { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsOfficial { get; set; }
    public int TotalSales { get; set; }
    public decimal? ResponseRate { get; set; }
    public int? ResponseTimeHours { get; set; }
    public string? StoreUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }

    // Navigation properties
    public Platform? Platform { get; set; }
    public ICollection<StoreListing> Listings { get; set; } = new List<StoreListing>();
}
