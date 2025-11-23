using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;
using System.Text.Json;

namespace PriceWatcher.Pages;

public class ProductDetailModel : PageModel
{
    private readonly PriceWatcherDbContext _dbContext;

    public ProductDetailModel(PriceWatcherDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Product Product { get; set; } = null!;
    public string PriceHistoryJson { get; set; } = "[]";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Product = await _dbContext.Products
            .Include(p => p.Platform)
            .Include(p => p.PriceSnapshots)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (Product == null)
        {
            return NotFound();
        }

        var history = Product.PriceSnapshots
            .OrderBy(s => s.RecordedAt)
            .Select(s => new { t = s.RecordedAt, y = s.Price })
            .ToList();

        PriceHistoryJson = JsonSerializer.Serialize(history);

        return Page();
    }
}
