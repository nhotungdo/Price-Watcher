using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        public int? PlatformId { get; set; }

        [StringLength(100)]
        public string? ExternalId { get; set; }

        [Required]
        [StringLength(500)]
        public string ProductName { get; set; } = null!;

        [Required]
        public string OriginalUrl { get; set; } = null!;

        public string? AffiliateUrl { get; set; }
        
        [StringLength(50)]
        public string? AffiliateProvider { get; set; }
        
        public DateTime? AffiliateExpiry { get; set; }

        public string? ImageUrl { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CurrentPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        public int? DiscountRate { get; set; }

        public string? StockStatus { get; set; } // 'InStock', 'OutOfStock'

        public double? Rating { get; set; }

        public int? ReviewCount { get; set; }

        public int? SoldQuantity { get; set; }

        [StringLength(200)]
        public string? ShopName { get; set; }

        public string? ShippingInfo { get; set; }

        public DateTime? LastUpdated { get; set; } = DateTime.Now;

        [ForeignKey("PlatformId")]
        public virtual Platform? Platform { get; set; }

        public virtual ICollection<PriceSnapshot> PriceSnapshots { get; set; } = new List<PriceSnapshot>();
        public virtual ICollection<PriceAlert> PriceAlerts { get; set; } = new List<PriceAlert>();
        public virtual ICollection<CrawlJob> CrawlJobs { get; set; } = new List<CrawlJob>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
