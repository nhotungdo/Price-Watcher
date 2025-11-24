using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class SearchHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int? UserId { get; set; }

        [StringLength(20)]
        public string SearchType { get; set; }

        public string InputContent { get; set; }

        [StringLength(200)]
        public string DetectedKeyword { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BestPriceFound { get; set; }

        public DateTime? SearchTime { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
