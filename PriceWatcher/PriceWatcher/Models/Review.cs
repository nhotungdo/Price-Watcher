using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Stars { get; set; }

        public string Content { get; set; }

        public bool IsVerifiedPurchase { get; set; } = false;

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
