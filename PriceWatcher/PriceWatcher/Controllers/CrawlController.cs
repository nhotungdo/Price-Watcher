using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PriceWatcher.Models;
using PriceWatcher.Services;
using PriceWatcher.Services.Scrapers;
using System;
using System.Threading.Tasks;

namespace PriceWatcher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrawlController : ControllerBase
    {
        private readonly ITikiCrawler _crawler;
        private readonly ICrawlQueue _queue;
        private readonly IMemoryCache _cache;
        private readonly PriceWatcherDbContext _context;

        public CrawlController(
            ITikiCrawler crawler,
            ICrawlQueue queue,
            IMemoryCache cache,
            PriceWatcherDbContext context)
        {
            _crawler = crawler;
            _queue = queue;
            _cache = cache;
            _context = context;
        }

        [HttpPost("tiki")]
        public async Task<IActionResult> CrawlTiki([FromBody] CrawlRequest request)
        {
            if (string.IsNullOrEmpty(request.Url) || !request.Url.Contains("tiki.vn"))
            {
                return BadRequest("Invalid Tiki URL");
            }

            // Check Cache
            var cacheKey = $"crawl_preview_{request.Url}";
            if (_cache.TryGetValue(cacheKey, out CrawledProduct cachedResult))
            {
                return Ok(new { productPreview = cachedResult, cached = true });
            }

            // Live Parse (Preview)
            var result = await _crawler.CrawlProductAsync(request.Url);

            if (result.IsSuccess)
            {
                // Cache result for 10 minutes
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

                // Enqueue for persistence
                _queue.Enqueue(new CrawlQueueItem
                {
                    Url = request.Url,
                    PlatformId = 1, // Assuming 1 is Tiki
                    QueuedAt = DateTime.Now
                });

                return Ok(new { productPreview = result, cached = false });
            }
            else
            {
                // If live parse fails, we can still queue it for retry or return error
                // For now, return Accepted if we want to try in background, or Error if we failed immediately.
                // The prompt suggests returning 202 if live parse fails but we queue it.
                // But if our crawler failed, likely the worker will fail too unless it's a transient issue.
                // Let's return 202 and queue it anyway.
                
                _queue.Enqueue(new CrawlQueueItem
                {
                    Url = request.Url,
                    PlatformId = 1,
                    QueuedAt = DateTime.Now
                });

                return Accepted(new { message = "Parsing queued; will persist when ready" });
            }
        }
    }

    public class CrawlRequest
    {
        public string Url { get; set; }
    }
}
