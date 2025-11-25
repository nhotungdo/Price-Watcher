using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PriceWatcher.Models;

namespace PriceWatcher.Pages.Admin.Products
{
    public class IndexModel : PageModel
    {
        private readonly PriceWatcherDbContext _db;
        private readonly IMemoryCache _cache;
        private const int PageSize = 20;

        public IndexModel(PriceWatcherDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        [BindProperty(SupportsGet = true)]
        public new int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? Query { get; set; }

        public List<Product> Products { get; private set; } = new();
        public bool HasMore { get; private set; }

        public async Task OnGetAsync()
        {
            var key = $"admin_products:{Page}:{Query}";
            if (!_cache.TryGetValue(key, out (List<Product> items, bool more) cached))
            {
                var q = _db.Products.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(Query))
                {
                    var term = Query.Trim();
                    q = q.Where(p => p.ProductName.Contains(term) || p.OriginalUrl.Contains(term));
                }

                var items = await q
                    .OrderByDescending(p => p.LastUpdated)
                    .Skip((Page - 1) * PageSize)
                    .Take(PageSize + 1)
                    .ToListAsync();

                var more = items.Count > PageSize;
                if (more) items.RemoveAt(items.Count - 1);

                cached = (items, more);
                _cache.Set(key, cached, TimeSpan.FromMinutes(1));
            }

            Products = cached.items;
            HasMore = cached.more;
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();
            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            return RedirectToPage(new { page = Page, q = Query });
        }
    }
}