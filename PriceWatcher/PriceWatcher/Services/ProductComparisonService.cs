using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PriceWatcher.Models;
using PriceWatcher.Dtos;

namespace PriceWatcher.Services
{
    public class ProductComparisonService : IProductComparisonService
    {
        private readonly PriceWatcherDbContext _context;
        private readonly IMemoryCache _cache;
        private const int CacheDurationMinutes = 5;

        public ProductComparisonService(PriceWatcherDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<List<ProductComparisonDto>> GetProductComparisonsAsync(
            int mappingId, 
            string currency = "VND", 
            string sort = "price", 
            bool onlyAvailable = false)
        {
            var cacheKey = $"comparison_{mappingId}_{currency}_{sort}_{onlyAvailable}";
            
            if (_cache.TryGetValue(cacheKey, out List<ProductComparisonDto> cachedResult))
            {
                return cachedResult;
            }

            // Get the mapping to find all related products
            var mapping = await _context.ProductMappings
                .FirstOrDefaultAsync(m => m.Id == mappingId);

            if (mapping == null)
            {
                return new List<ProductComparisonDto>();
            }

            // Parse matched candidates JSON to get product IDs
            // For now, we'll search by similar product names as a fallback
            var products = await _context.Products
                .Include(p => p.Platform)
                .Include(p => p.PriceSnapshots)
                .Where(p => p.ProductName.Contains(mapping.SourceProductId ?? ""))
                .ToListAsync();

            var comparisons = products.Select(p => new ProductComparisonDto
            {
                ProductId = p.ProductId,
                PlatformId = p.PlatformId ?? 0,
                PlatformName = p.Platform?.PlatformName ?? "",
                PlatformLogo = $"/images/platforms/{p.Platform?.PlatformName?.ToLower()}.png",
                Domain = p.Platform?.Domain ?? "",
                ColorCode = p.Platform?.ColorCode ?? "#000000",
                ShopName = p.ShopName ?? "",
                Price = p.CurrentPrice,
                LastUpdated = p.LastUpdated,
                AffiliateUrl = p.AffiliateUrl ?? "",
                OriginalUrl = p.OriginalUrl,
                ShippingInfo = "Free shipping", // TODO: Implement shipping calculation
                IsCheapest = false,
                Rating = p.Rating,
                ReviewCount = p.ReviewCount,
                ProductName = p.ProductName,
                ImageUrl = p.ImageUrl
            }).ToList();

            // Filter only available products
            if (onlyAvailable)
            {
                comparisons = comparisons.Where(c => c.Price.HasValue && c.Price > 0).ToList();
            }

            // Determine cheapest
            if (comparisons.Any(c => c.Price.HasValue))
            {
                var minPrice = comparisons.Where(c => c.Price.HasValue).Min(c => c.Price);
                foreach (var comparison in comparisons.Where(c => c.Price == minPrice))
                {
                    comparison.IsCheapest = true;
                }
            }

            // Sort
            comparisons = sort.ToLower() switch
            {
                "rating" => comparisons.OrderByDescending(c => c.Rating).ToList(),
                "price" => comparisons.OrderBy(c => c.Price ?? decimal.MaxValue).ToList(),
                _ => comparisons.OrderBy(c => c.Price ?? decimal.MaxValue).ToList()
            };

            // Cache the result
            _cache.Set(cacheKey, comparisons, TimeSpan.FromMinutes(CacheDurationMinutes));

            return comparisons;
        }

        public async Task<List<ProductComparisonDto>> GetProductComparisonsByProductIdAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Platform)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return new List<ProductComparisonDto>();
            }

            // Find similar products across platforms
            var similarProducts = await _context.Products
                .Include(p => p.Platform)
                .Where(p => p.ProductName.Contains(product.ProductName.Substring(0, Math.Min(20, product.ProductName.Length))))
                .ToListAsync();

            var comparisons = similarProducts.Select(p => new ProductComparisonDto
            {
                ProductId = p.ProductId,
                PlatformId = p.PlatformId ?? 0,
                PlatformName = p.Platform?.PlatformName ?? "",
                PlatformLogo = $"/images/platforms/{p.Platform?.PlatformName?.ToLower()}.png",
                Domain = p.Platform?.Domain ?? "",
                ColorCode = p.Platform?.ColorCode ?? "#000000",
                ShopName = p.ShopName ?? "",
                Price = p.CurrentPrice,
                LastUpdated = p.LastUpdated,
                AffiliateUrl = p.AffiliateUrl ?? "",
                OriginalUrl = p.OriginalUrl,
                ShippingInfo = "Free shipping",
                IsCheapest = false,
                Rating = p.Rating,
                ReviewCount = p.ReviewCount,
                ProductName = p.ProductName,
                ImageUrl = p.ImageUrl
            }).ToList();

            // Determine cheapest
            if (comparisons.Any(c => c.Price.HasValue))
            {
                var minPrice = comparisons.Where(c => c.Price.HasValue).Min(c => c.Price);
                foreach (var comparison in comparisons.Where(c => c.Price == minPrice))
                {
                    comparison.IsCheapest = true;
                }
            }

            return comparisons.OrderBy(c => c.Price ?? decimal.MaxValue).ToList();
        }
    }
}
