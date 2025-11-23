using System.Collections.Concurrent;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class SearchStatusService : ISearchStatusService
{
    private readonly ConcurrentDictionary<Guid, SearchStatusDto> _statuses = new();
    private static readonly ConcurrentDictionary<string, List<(DateTime ts, decimal price)>> _priceHistory = new();

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
        var warnings = new List<string>();
        if (originalPrice.HasValue)
        {
            var op = originalPrice.Value;
            if (arr.Any(r => r.Price >= 2 * op)) warnings.Add("Giá cao bất thường (>= 2x giá gốc)");
            if (arr.Any(r => r.Price > 0 && r.Price <= 0.5m * op)) warnings.Add("Giá thấp bất thường (<= 50% giá gốc)");
        }
        if (arr.Any(r => r.IsOutOfStock == true)) warnings.Add("Một số sản phẩm hết hàng");
        var best = arr.OrderBy(r => r.TotalCost).FirstOrDefault();
        var histKey = !string.IsNullOrWhiteSpace(query.CanonicalUrl) ? query.CanonicalUrl : (query.Platform + ":" + query.ProductId);
        if (!string.IsNullOrWhiteSpace(histKey) && best != null && best.Price > 0)
        {
            var list = _priceHistory.GetOrAdd(histKey, _ => new List<(DateTime, decimal)>());
            list.Add((DateTime.UtcNow, best.Price));
            if (list.Count > 50) list.RemoveRange(0, list.Count - 50);
        }
        var historyPrices = (!string.IsNullOrWhiteSpace(histKey) && _priceHistory.TryGetValue(histKey, out var hp)) ? hp.Select(x => x.price).OrderBy(x => x).ToArray() : Array.Empty<decimal>();
        string? advice = null;
        if (historyPrices.Length >= 8)
        {
            var p25 = historyPrices[(int)Math.Floor(0.25 * (historyPrices.Length - 1))];
            var p75 = historyPrices[(int)Math.Floor(0.75 * (historyPrices.Length - 1))];
            if (best != null)
            {
                if (best.Price <= p25) advice = "Mua ngay: giá đang thuộc vùng thấp lịch sử";
                else if (best.Price >= p75) advice = "Nên chờ thêm: giá thuộc vùng cao lịch sử";
            }
        }
        if (advice == null && best != null && best.DiscountPercent.HasValue)
        {
            var pct = best.DiscountPercent.Value;
            if (pct >= 0.3) advice = "Khuyến nghị mua ngay: mức giảm sâu";
            else if (pct >= 0.15) advice = "Mức giảm tốt, có thể cân nhắc mua";
            else advice = "Giảm ít, có thể chờ thêm khuyến mãi";
        }
        if (advice == null)
        {
            advice = "Chưa có dữ liệu lịch sử để gợi ý thời điểm mua";
        }
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
            Higher = higher,
            Advice = advice,
            Warnings = warnings.Count > 0 ? warnings : null
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

