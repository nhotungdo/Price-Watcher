using System.ComponentModel.DataAnnotations;

namespace PriceWatcher.Dtos;

public class AddCartItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 99)]
    public int Quantity { get; set; } = 1;

    [Required, StringLength(500)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public decimal Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public int? PlatformId { get; set; }

    [StringLength(100)]
    public string? PlatformName { get; set; }

    [StringLength(1000)]
    public string? ImageUrl { get; set; }

    [StringLength(1000)]
    public string? ProductUrl { get; set; }
}

