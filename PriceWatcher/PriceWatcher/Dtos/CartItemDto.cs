namespace PriceWatcher.Dtos;

public class CartItemDto
{
    public int CartItemId { get; set; }
    public int? ProductId { get; set; }
    public int? PlatformId { get; set; }
    public string? PlatformName { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public int Quantity { get; set; }
}

