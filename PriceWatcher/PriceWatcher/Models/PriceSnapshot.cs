using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class PriceSnapshot
    {
        [Key]
        public int SnapshotId { get; set; }

        public int? ProductId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        public string ShippingInfo { get; set; }

        public DateTime? RecordedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
