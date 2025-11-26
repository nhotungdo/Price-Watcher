namespace PriceWatcher.Models;

public class ProductNews
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? SourceUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? Author { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Product? Product { get; set; }
}
