using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;

namespace PriceWatcher.Pages.Admin.Products
{
    public class EditModel : PageModel
    {
        private readonly PriceWatcherDbContext _db;

        public EditModel(PriceWatcherDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public EditInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == id);
            if (p == null) return NotFound();
            Input = new EditInput
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                CurrentPrice = p.CurrentPrice,
                OriginalUrl = p.OriginalUrl,
                ImageUrl = p.ImageUrl
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!CreateModel.IsValidTikiUrl(Input.OriginalUrl))
            {
                ModelState.AddModelError(nameof(Input.OriginalUrl), "URL Tiki không hợp lệ");
            }
            if (!ModelState.IsValid) return Page();

            var p = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == Input.ProductId);
            if (p == null) return NotFound();
            p.ProductName = Input.ProductName;
            p.Description = Input.Description;
            p.CurrentPrice = Input.CurrentPrice;
            p.OriginalUrl = Input.OriginalUrl;
            p.ImageUrl = Input.ImageUrl;
            p.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }

        public class EditInput
        {
            public int ProductId { get; set; }

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