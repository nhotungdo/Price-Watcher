using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceWatcher.Dtos;
using PriceWatcher.Options;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IEnumerable<IProductScraper> _scrapers;
    private readonly RecommendationOptions _options;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(IEnumerable<IProductScraper> scrapers, IOptions<RecommendationOptions> options, ILogger<RecommendationService> logger)
    {
        _scrapers = scrapers;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductCandidateDto>> RecommendAsync(ProductQuery query, int top = 3, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var gatherTasks = new List<Task<IEnumerable<ProductCandidateDto>>>();
        var platformScraper = _scrapers.FirstOrDefault(s => string.Equals(s.Platform, query.Platform, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.CanonicalUrl))
        {
            if (platformScraper != null)
            {
                gatherTasks.Add(GatherFromScraper(platformScraper, query, cancellationToken));
                var byUrl = await SafeGetByUrl(platformScraper, query, cancellationToken);
                if (byUrl != null)
                {
                    gatherTasks.Add(Task.FromResult<IEnumerable<ProductCandidateDto>>(new[] { byUrl }));
                }
            }
        }
        else
        {
            foreach (var scraper in _scrapers)
            {
                gatherTasks.Add(GatherFromScraper(scraper, query, cancellationToken));
            }
        }

        var gathered = await Task.WhenAll(gatherTasks);
        var candidates = gathered.SelectMany(x => x).ToList();

        if (!candidates.Any())
        {
            // Fallback: if we have a meaningful title hint, try cross-platform search even when a canonical URL is present
            if (!string.IsNullOrWhiteSpace(query.TitleHint))
            {
                var crossTasks = new List<Task<IEnumerable<ProductCandidateDto>>>();
                foreach (var scraper in _scrapers)
                {
                    crossTasks.Add(GatherFromScraper(scraper, query, cancellationToken));
                }
                var cross = await Task.WhenAll(crossTasks);
                candidates = cross.SelectMany(x => x).ToList();
            }

            if (!candidates.Any())
            {
                _logger.LogWarning("No candidates gathered for {@Query}", query);
                return Array.Empty<ProductCandidateDto>();
            }
        }

        var medianPrice = CalculateMedian(candidates.Select(c => c.Price));
        var filtered = candidates
            .Where(c => c.Price >= 0.3m * medianPrice)
            .Where(c => c.ShopRating > 0)
            .ToList();

        if (!filtered.Any())
        {
            filtered = candidates.Where(c => c.Price > 0).ToList();
            if (!filtered.Any()) filtered = candidates.ToList();
            _logger.LogWarning("All candidates filtered by strict rules; using relaxed filtering for {@Query}", query);
        }

        var title = (query.TitleHint ?? string.Empty).ToLowerInvariant();
        var scored = ScoreCandidates(filtered, title);
        var orderedByScore = scored.OrderByDescending(c => c.score).Select(c => c.candidate).ToList();
        var ordered = orderedByScore.OrderBy(c => c.TotalCost).ToList();
        LabelCandidates(ordered);

        sw.Stop();
        return ordered.Take(top).ToList();
    }

    private async Task<IEnumerable<ProductCandidateDto>> GatherFromScraper(IProductScraper scraper, ProductQuery query, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var res = await scraper.SearchByQueryAsync(query, cancellationToken);
            sw.Stop();
            _logger.LogInformation("Scraper {Platform} returned {Count} items in {Elapsed} ms", scraper.Platform, res.Count(), sw.ElapsedMilliseconds);
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scraper {Platform} failed", scraper.Platform);
            return Array.Empty<ProductCandidateDto>();
        }
    }

    private async Task<ProductCandidateDto?> SafeGetByUrl(IProductScraper scraper, ProductQuery query, CancellationToken cancellationToken)
    {
        try
        {
            return await scraper.GetByUrlAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetByUrl failed for {Platform}", scraper.Platform);
            return null;
        }
    }

    private static decimal CalculateMedian(IEnumerable<decimal> values)
    {
        var ordered = values.OrderBy(v => v).ToArray();
        if (ordered.Length == 0)
        {
            return 0;
        }

        var middle = ordered.Length / 2;
        if (ordered.Length % 2 == 0)
        {
            return (ordered[middle - 1] + ordered[middle]) / 2;
        }

        return ordered[middle];
    }

    private IEnumerable<(ProductCandidateDto candidate, decimal score)> ScoreCandidates(IEnumerable<ProductCandidateDto> candidates, string titleHint)
    {
        var maxTotal = candidates.Max(c => c.TotalCost);
        var maxShipping = candidates.Max(c => c.ShippingCost);

        foreach (var candidate in candidates)
        {
            var priceScore = maxTotal == 0 ? 0 : 1 - (candidate.TotalCost / maxTotal);
            var ratingScore = (decimal)(candidate.ShopRating / 5.0);
            var shippingScore = maxShipping == 0 ? 1 : 1 - (candidate.ShippingCost / maxShipping);
            var titleScore = ComputeTitleSimilarity(titleHint, candidate.Title);

            var score = priceScore * _options.WeightPrice
                        + ratingScore * _options.WeightRating
                        + shippingScore * _options.WeightShipping
                        + titleScore * _options.WeightTitleSimilarity;

            candidate.MatchScore = score;
            candidate.FitReason = BuildReason(priceScore, ratingScore, shippingScore, titleScore);
            yield return (candidate, score);
        }
    }

    private static decimal ComputeTitleSimilarity(string hint, string title)
    {
        if (string.IsNullOrWhiteSpace(hint)) return 0;
        var a = hint.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant()).Distinct().ToHashSet();
        var b = (title ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant()).Distinct().ToHashSet();
        if (a.Count == 0 || b.Count == 0) return 0;
        var inter = a.Intersect(b).Count();
        var union = a.Union(b).Count();
        return union == 0 ? 0 : (decimal)inter / union;
    }

    private static string BuildReason(decimal priceScore, decimal ratingScore, decimal shippingScore, decimal titleScore)
    {
        var reasons = new List<string>();
        if (priceScore >= 0.6m) reasons.Add("Giá tốt");
        if (ratingScore >= 0.8m) reasons.Add("Shop uy tín");
        if (titleScore >= 0.3m) reasons.Add("Tên khớp");
        if (shippingScore >= 0.6m) reasons.Add("Phí ship thấp");
        return string.Join(", ", reasons);
    }

    private void LabelCandidates(IList<ProductCandidateDto> candidates)
    {
        if (candidates.Count == 0)
        {
            return;
        }

        var bestDeal = candidates.OrderBy(c => c.TotalCost).First();
        AssignLabels(bestDeal, new[] { "BestDeal" });

        foreach (var candidate in candidates)
        {
            var labels = new List<string>();
            if (candidate == bestDeal)
            {
                labels.Add("BestDeal");
            }

            if (candidate.ShopRating > 4.8 && candidate.ShopSales >= _options.TrustedShopSalesThreshold)
            {
                labels.Add("TrustedShop");
            }

            if (candidate.ShopName.Contains("Official", StringComparison.OrdinalIgnoreCase)
                || candidate.ShopName.Contains("Mall", StringComparison.OrdinalIgnoreCase))
            {
                labels.Add("OfficialStore");
            }

            AssignLabels(candidate, labels);
        }
    }

    private static void AssignLabels(ProductCandidateDto candidate, IEnumerable<string> labels)
    {
        candidate.Labels = labels.Distinct().ToArray();
    }
}

