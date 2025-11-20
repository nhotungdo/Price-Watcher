using System.Collections.Concurrent;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class SearchStatusService : ISearchStatusService
{
    private readonly ConcurrentDictionary<Guid, SearchStatusDto> _statuses = new();

    public void Initialize(Guid searchId)
    {
        _statuses[searchId] = new SearchStatusDto
        {
            SearchId = searchId,
            Status = "Pending",
            Message = "Waiting for processing"
        };
    }

    public void MarkProcessing(Guid searchId)
    {
        _statuses.AddOrUpdate(searchId,
            id => new SearchStatusDto { SearchId = id, Status = "Processing" },
            (_, existing) =>
            {
                existing.Status = "Processing";
                existing.Message = "Processing";
                return existing;
            });
    }

    public void Complete(Guid searchId, ProductQuery query, IEnumerable<ProductCandidateDto> results)
    {
        var arr = results.ToArray();
        var samePlatform = arr.Where(r => string.Equals(r.Platform, query.Platform, StringComparison.OrdinalIgnoreCase)).ToArray();
        var baselinePool = samePlatform.Length > 0 ? samePlatform : arr;
        decimal? originalPrice = null;
        if (baselinePool.Length > 0)
        {
            var prices = baselinePool.Select(r => r.Price).OrderBy(p => p).ToArray();
            var mid = prices.Length / 2;
            originalPrice = prices.Length % 2 == 0 ? (prices[mid - 1] + prices[mid]) / 2 : prices[mid];
        }
        var lower = originalPrice.HasValue
            ? arr.Where(r => r.TotalCost < originalPrice.Value).OrderBy(r => r.TotalCost).ToArray()
            : Array.Empty<ProductCandidateDto>();
        var higher = originalPrice.HasValue
            ? arr.Where(r => r.TotalCost >= originalPrice.Value).OrderBy(r => r.TotalCost).ToArray()
            : Array.Empty<ProductCandidateDto>();

        var firstImage = samePlatform.FirstOrDefault()?.ThumbnailUrl ?? arr.FirstOrDefault()?.ThumbnailUrl;
        var dto = new SearchStatusDto
        {
            SearchId = searchId,
            Status = "Completed",
            Results = arr.OrderBy(r => r.TotalCost).ToArray(),
            OriginalPrice = originalPrice,
            ProductName = query.TitleHint,
            ProductImageUrl = firstImage,
            Category = InferCategory(query.TitleHint),
            Lower = lower,
            Higher = higher
        };
        _statuses[searchId] = dto;
    }

    public void Fail(Guid searchId, string message)
    {
        _statuses[searchId] = new SearchStatusDto
        {
            SearchId = searchId,
            Status = "Failed",
            Message = message
        };
    }

    public SearchStatusDto? GetStatus(Guid searchId)
    {
        _statuses.TryGetValue(searchId, out var status);
        return status;
    }

    private static string? InferCategory(string? title)
    {
        var t = (title ?? string.Empty).ToLowerInvariant();
        if (t.Contains("iphone") || t.Contains("kính cường lực") || t.Contains("ốp") || t.Contains("sạc")) return "Điện thoại & Phụ kiện";
        if (t.Contains("laptop") || t.Contains("chuột") || t.Contains("bàn phím")) return "Máy tính & Phụ kiện";
        if (t.Contains("áo") || t.Contains("quần") || t.Contains("giày")) return "Thời trang";
        return null;
    }
}

