using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        [StringLength(100)]
        public string? GoogleId { get; set; }

        public byte[]? PasswordHash { get; set; }

        public byte[]? PasswordSalt { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLogin { get; set; }

        public virtual ICollection<SearchHistory>? SearchHistories { get; set; }
        public virtual ICollection<PriceAlert>? PriceAlerts { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; }
    }
}
