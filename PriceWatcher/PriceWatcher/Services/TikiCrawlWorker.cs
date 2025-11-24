using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceWatcher.Models;
using PriceWatcher.Services.Scrapers;

namespace PriceWatcher.Services
{
    public class TikiCrawlWorker : BackgroundService
    {
        private readonly ICrawlQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TikiCrawlWorker> _logger;

        public TikiCrawlWorker(
            ICrawlQueue queue,
            IServiceProvider serviceProvider,
            ILogger<TikiCrawlWorker> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TikiCrawlWorker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _queue.DequeueAsync(stoppingToken);
                    if (job == null) continue;

                    _logger.LogInformation("Processing crawl job for URL: {Url}", job.Url);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var crawler = scope.ServiceProvider.GetRequiredService<ITikiCrawler>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<PriceWatcherDbContext>();

                        var crawledData = await crawler.CrawlProductAsync(job.Url);

                        if (crawledData.IsSuccess)
                        {
                            await SaveProductAsync(dbContext, crawledData, job.PlatformId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to crawl product: {Url}. Error: {Error}", job.Url, crawledData.ErrorMessage);
                            // TODO: Implement retry logic here (re-enqueue with retry count)
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing crawl job.");
                }
            }
        }

        private async Task SaveProductAsync(PriceWatcherDbContext db, CrawledProduct data, int platformId)
        {
            // Check if product exists
            var product = await db.Products
                .FirstOrDefaultAsync(p => p.PlatformId == platformId && p.ExternalId == data.SourceProductId);

            if (product == null)
            {
                product = new Product
                {
                    PlatformId = platformId,
                    ExternalId = data.SourceProductId,
                    ProductName = data.Title,
                    OriginalUrl = $"https://tiki.vn/product-p{data.SourceProductId}.html", // Reconstruct or use original
                    ImageUrl = data.MainImageUrl,
                    CurrentPrice = data.Price,
                    OriginalPrice = data.OriginalPrice,
                    DiscountRate = data.DiscountPercent,
                    Rating = data.Rating,
                    ReviewCount = data.ReviewCount,
                    SoldQuantity = data.SoldQuantity,
                    ShopName = data.ShopName,
                    StockStatus = data.StockStatus,
                    LastUpdated = DateTime.Now
                };
                db.Products.Add(product);
            }
            else
            {
                // Update existing
                product.ProductName = data.Title;
                product.CurrentPrice = data.Price;
                product.OriginalPrice = data.OriginalPrice;
                product.DiscountRate = data.DiscountPercent;
                product.ImageUrl = data.MainImageUrl;
                product.Rating = data.Rating;
                product.ReviewCount = data.ReviewCount;
                product.SoldQuantity = data.SoldQuantity;
                product.ShopName = data.ShopName;
                product.StockStatus = data.StockStatus;
                product.LastUpdated = DateTime.Now;
            }

            await db.SaveChangesAsync();

            // Add Price Snapshot
            var snapshot = new PriceSnapshot
            {
                ProductId = product.ProductId,
                Price = data.Price,
                OriginalPrice = data.OriginalPrice,
                RecordedAt = DateTime.Now
            };
            db.PriceSnapshots.Add(snapshot);
            await db.SaveChangesAsync();

            _logger.LogInformation("Successfully saved product: {ProductName}", product.ProductName);
        }
    }
}
