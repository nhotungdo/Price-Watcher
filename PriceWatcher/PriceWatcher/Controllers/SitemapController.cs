using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;
using System.Text;
using System.Xml;

namespace PriceWatcher.Controllers
{
    [Route("sitemap.xml")]
    [ApiController]
    public class SitemapController : ControllerBase
    {
        private readonly PriceWatcherDbContext _context;
        private readonly ILogger<SitemapController> _logger;

        public SitemapController(PriceWatcherDbContext context, ILogger<SitemapController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Produces("application/xml")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var sitemap = new StringBuilder();

                sitemap.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sitemap.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

                // Homepage
                AddUrl(sitemap, baseUrl, DateTime.UtcNow, "daily", "1.0");

                // Static pages
                AddUrl(sitemap, $"{baseUrl}/search/results", DateTime.UtcNow, "daily", "0.9");
                AddUrl(sitemap, $"{baseUrl}/contact", DateTime.UtcNow, "monthly", "0.7");
                AddUrl(sitemap, $"{baseUrl}/login", DateTime.UtcNow, "monthly", "0.5");
                AddUrl(sitemap, $"{baseUrl}/register", DateTime.UtcNow, "monthly", "0.5");

                // Categories
                var categories = new[] { "electronics", "laptop", "camera", "audio", "watch", "home", "books", "fashion" };
                foreach (var category in categories)
                {
                    AddUrl(sitemap, $"{baseUrl}/category/{category}", DateTime.UtcNow, "weekly", "0.8");
                }

                // Products (limit to recent 1000 for performance)
                var products = await _context.Products
                    .OrderByDescending(p => p.LastUpdated)
                    .Take(1000)
                    .Select(p => new { p.ProductId, p.LastUpdated })
                    .ToListAsync();

                foreach (var product in products)
                {
                    AddUrl(sitemap, $"{baseUrl}/ProductDetail/{product.ProductId}", 
                        product.LastUpdated ?? DateTime.UtcNow, "weekly", "0.6");
                }

                sitemap.AppendLine("</urlset>");

                return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sitemap");
                return StatusCode(500);
            }
        }

        private void AddUrl(StringBuilder sitemap, string loc, DateTime lastmod, string changefreq, string priority)
        {
            sitemap.AppendLine("  <url>");
            sitemap.AppendLine($"    <loc>{XmlEscape(loc)}</loc>");
            sitemap.AppendLine($"    <lastmod>{lastmod:yyyy-MM-dd}</lastmod>");
            sitemap.AppendLine($"    <changefreq>{changefreq}</changefreq>");
            sitemap.AppendLine($"    <priority>{priority}</priority>");
            sitemap.AppendLine("  </url>");
        }

        private string XmlEscape(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
