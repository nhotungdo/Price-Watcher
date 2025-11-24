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
    public class PriceAnalyticsService : IPriceAnalyticsService
    {
        private readonly PriceWatcherDbContext _context;
        private readonly IMemoryCache _cache;

        public PriceAnalyticsService(PriceWatcherDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<PriceHistoryResponseDto> GetPriceHistoryAsync(int productId, int days = 90, string granularity = "daily")
        {
            var cacheKey = $"price_history_{productId}_{days}_{granularity}";
            
            if (_cache.TryGetValue(cacheKey, out PriceHistoryResponseDto cachedResult))
            {
                return cachedResult;
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return null;
            }

            var cutoffDate = DateTime.Now.AddDays(-days);
            var snapshots = await _context.PriceSnapshots
                .Where(ps => ps.ProductId == productId && ps.RecordedAt >= cutoffDate)
                .OrderBy(ps => ps.RecordedAt)
                .ToListAsync();

            if (snapshots.Count < 7)
            {
                return new PriceHistoryResponseDto
                {
                    ProductId = productId,
                    ProductName = product.ProductName,
                    History = new List<PriceHistoryDto>(),
                    CurrentPrice = product.CurrentPrice,
                    BestPrice = null,
                    BestPriceDate = null
                };
            }

            // Group by date and aggregate
            var groupedData = snapshots
                .GroupBy(ps => ps.RecordedAt.Value.Date)
                .Select(g => new PriceHistoryDto
                {
                    Date = g.Key,
                    MinPrice = g.Min(ps => ps.Price),
                    AvgPrice = g.Average(ps => ps.Price),
                    MaxPrice = g.Max(ps => ps.Price),
                    SampleCount = g.Count()
                })
                .OrderBy(h => h.Date)
                .ToList();

            var bestSnapshot = snapshots.OrderBy(ps => ps.Price).FirstOrDefault();

            var result = new PriceHistoryResponseDto
            {
                ProductId = productId,
                ProductName = product.ProductName,
                History = groupedData,
                CurrentPrice = product.CurrentPrice,
                BestPrice = bestSnapshot?.Price,
                BestPriceDate = bestSnapshot?.RecordedAt
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

            return result;
        }

        public async Task<BuySignalDto> GetBuySignalAsync(int productId, int horizon = 7)
        {
            var cacheKey = $"buy_signal_{productId}_{horizon}";
            
            if (_cache.TryGetValue(cacheKey, out BuySignalDto cachedResult))
            {
                return cachedResult;
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.CurrentPrice.HasValue)
            {
                return new BuySignalDto
                {
                    Signal = "UNKNOWN",
                    Confidence = 0,
                    Reason = "Product not found or price not available",
                    CurrentPrice = 0
                };
            }

            var cutoffDate = DateTime.Now.AddDays(-90);
            var prices = await _context.PriceSnapshots
                .Where(ps => ps.ProductId == productId && ps.RecordedAt >= cutoffDate)
                .Select(ps => ps.Price)
                .ToListAsync();

            if (prices.Count < 7)
            {
                return new BuySignalDto
                {
                    Signal = "UNKNOWN",
                    Confidence = 0,
                    Reason = "Not enough data to provide a recommendation",
                    CurrentPrice = product.CurrentPrice.Value
                };
            }

            var currentPrice = product.CurrentPrice.Value;
            var sortedPrices = prices.OrderBy(p => p).ToList();
            
            // Calculate percentiles
            var percentile10Index = (int)(sortedPrices.Count * 0.1);
            var medianIndex = sortedPrices.Count / 2;
            
            var percentile10 = sortedPrices[percentile10Index];
            var median = sortedPrices[medianIndex];
            var bestPrice = sortedPrices.First();

            string signal;
            double confidence;
            string reason;

            if (currentPrice <= percentile10)
            {
                signal = "GOOD";
                var percentBelow = ((percentile10 - currentPrice) / percentile10) * 100;
                confidence = Math.Min(0.95, 0.7 + (double)(percentBelow / 100));
                reason = $"Current price is {percentBelow:F1}% below the 10th percentile (₫{percentile10:N0})";
            }
            else if (currentPrice > median * 1.2m)
            {
                signal = "HIGH";
                var percentAbove = ((currentPrice - median) / median) * 100;
                confidence = Math.Min(0.9, 0.6 + (double)(percentAbove / 200));
                reason = $"Current price is {percentAbove:F1}% above the median (₫{median:N0})";
            }
            else
            {
                signal = "HOLD";
                confidence = 0.6;
                reason = $"Price is stable around the median (₫{median:N0})";
            }

            var result = new BuySignalDto
            {
                Signal = signal,
                Confidence = confidence,
                Reason = reason,
                CurrentPrice = currentPrice,
                Percentile10 = percentile10,
                Median = median,
                BestPrice = bestPrice
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));

            return result;
        }
    }
}
