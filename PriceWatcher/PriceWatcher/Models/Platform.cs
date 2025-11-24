using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class Platform
    {
        [Key]
        public int PlatformId { get; set; }

        [Required]
        [StringLength(50)]
        public string PlatformName { get; set; } = null!;

        [StringLength(100)]
        public string? Domain { get; set; }

        [StringLength(20)]
        public string? ColorCode { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<CrawlJob> CrawlJobs { get; set; } = new List<CrawlJob>();
    }
}
