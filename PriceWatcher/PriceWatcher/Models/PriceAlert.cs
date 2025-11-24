using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class PriceAlert
    {
        [Key]
        public int AlertId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetPrice { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastNotifiedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
