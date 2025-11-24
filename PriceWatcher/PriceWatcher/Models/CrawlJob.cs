using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class CrawlJob
    {
        [Key]
        public int JobId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int PlatformId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime? LastTriedAt { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("PlatformId")]
        public virtual Platform Platform { get; set; } = null!;
    }
}
