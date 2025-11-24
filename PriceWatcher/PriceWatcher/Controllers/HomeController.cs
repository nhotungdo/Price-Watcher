using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PriceWatcher.Controllers
{
    [Route("api/home")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly PriceWatcherDbContext _context;

        public HomeController(PriceWatcherDbContext context)
        {
            _context = context;
        }

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 12)
        {
            var products = await _context.Products
                .Include(p => p.Platform)
                .OrderByDescending(p => p.LastUpdated)
                .Take(limit)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.CurrentPrice,
                    p.ImageUrl,
                    p.ShopName,
                    p.Rating,
                    p.ReviewCount,
                    p.OriginalUrl,
                    PlatformName = p.Platform != null ? p.Platform.PlatformName : null,
                    p.LastUpdated
                })
                .ToListAsync();

            return Ok(products);
        }
    }
}
