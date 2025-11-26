namespace PriceWatcher.Models;

public class Favorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public string? CollectionName { get; set; }
    public string? Notes { get; set; }
    public decimal? TargetPrice { get; set; }
    public bool NotifyOnPriceDrop { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastViewedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Product? Product { get; set; }
}
