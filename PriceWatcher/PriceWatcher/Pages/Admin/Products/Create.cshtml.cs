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

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(IFormFile? ImageFile)
        {
            if (!IsValidTikiUrl(Input.OriginalUrl))
            {
                ModelState.AddModelError(nameof(Input.OriginalUrl), "URL Tiki không hợp lệ");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? imageUrl = Input.ImageUrl;
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var ext = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError(string.Empty, "Định dạng ảnh không hỗ trợ");
                    return Page();
                }
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploads);
                var fname = $"p_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
                var path = Path.Combine(uploads, fname);
                using (var stream = System.IO.File.Create(path))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                imageUrl = $"/uploads/{fname}";
            }

            var p = new Product
            {
                ProductName = Input.ProductName,
                Description = Input.Description,
                CurrentPrice = Input.CurrentPrice,
                OriginalUrl = Input.OriginalUrl,
                ImageUrl = imageUrl,
                LastUpdated = DateTime.UtcNow
            };

            _db.Products.Add(p);
            await _db.SaveChangesAsync();

            return RedirectToPage("Index");
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