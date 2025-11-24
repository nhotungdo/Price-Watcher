using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(200)]
        public string CategoryName { get; set; }

        public int? ParentCategoryId { get; set; }

        [StringLength(500)]
        public string IconUrl { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ParentCategoryId")]
        public virtual Category ParentCategory { get; set; }

        public virtual ICollection<Category> SubCategories { get; set; }
    }
}
