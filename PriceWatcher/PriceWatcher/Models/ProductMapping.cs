using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class ProductMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string SourceUrl { get; set; }

        [StringLength(200)]
        public string SourceProductId { get; set; }

        public string MatchedCandidatesJson { get; set; }

        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}
