using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models
{
    public class SystemLog
    {
        [Key]
        public int LogId { get; set; }

        [StringLength(20)]
        public string Level { get; set; }

        public string Message { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
