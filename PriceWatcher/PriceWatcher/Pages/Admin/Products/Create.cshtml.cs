using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PriceWatcher.Models;

namespace PriceWatcher.Pages.Admin.Products
{
    public class CreateModel : PageModel
    {
        private readonly PriceWatcherDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CreateModel(PriceWatcherDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [BindProperty]
        public CreateInput Input { get; set; } = new();

        public IActionResult OnGet()
        {
            return NotFound();
        }

        public Task<IActionResult> OnPostAsync(IFormFile? ImageFile)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        public static bool IsValidTikiUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (!uri.Host.Contains("tiki.vn", StringComparison.OrdinalIgnoreCase)) return false;
            var pathOk = System.Text.RegularExpressions.Regex.IsMatch(uri.AbsolutePath, @"-p\d+\.html", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var hasSpid = uri.Query.Contains("spid=", StringComparison.OrdinalIgnoreCase);
            return pathOk || hasSpid;
        }

        public class CreateInput
        {
            [Required]
            [StringLength(500)]
            public string ProductName { get; set; } = string.Empty;

            public string? Description { get; set; }

            [Range(0, 1000000000)]
            public decimal? CurrentPrice { get; set; }

            [Required]
            [Url]
            public string OriginalUrl { get; set; } = string.Empty;

            public string? ImageUrl { get; set; }
        }
    }
}
