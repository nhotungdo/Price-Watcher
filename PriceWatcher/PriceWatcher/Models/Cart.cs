using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models;

public class Cart
{
    [Key]
    public int CartId { get; set; }

    public int? UserId { get; set; }

    public Guid? AnonymousId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}

