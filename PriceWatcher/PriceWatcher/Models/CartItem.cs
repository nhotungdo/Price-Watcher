using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models;

public class CartItem
{
    [Key]
    public int CartItemId { get; set; }

    public int CartId { get; set; }

    public int? ProductId { get; set; }

    [StringLength(500)]
    public string ProductName { get; set; } = string.Empty;

    public int? PlatformId { get; set; }

    [StringLength(100)]
    public string? PlatformName { get; set; }

    [StringLength(1000)]
    public string? ImageUrl { get; set; }

    [StringLength(1000)]
    public string? ProductUrl { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? OriginalPrice { get; set; }

    public int Quantity { get; set; } = 1;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? MetadataJson { get; set; }

    [ForeignKey(nameof(CartId))]
    public virtual Cart Cart { get; set; } = null!;
}

