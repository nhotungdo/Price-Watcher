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
        var gatherTasks = _scrapers.Select(scraper => GatherFromScraper(scraper, query, cancellationToken));
        var gathered = await Task.WhenAll(gatherTasks);
        var candidates = gathered.SelectMany(x => x).ToList();

        if (!candidates.Any())
        {
            _logger.LogWarning("No candidates gathered for {@Query}", query);
            return Array.Empty<ProductCandidateDto>();
        }

        var medianPrice = CalculateMedian(candidates.Select(c => c.Price));
        var filtered = candidates
            .Where(c => c.Price >= 0.3m * medianPrice)
            .Where(c => c.ShopRating > 0)
            .ToList();

        if (!filtered.Any())
        {
            _logger.LogWarning("All candidates filtered out for {@Query}", query);
            return Array.Empty<ProductCandidateDto>();
        }

        var scored = ScoreCandidates(filtered);
        var ordered = scored.OrderByDescending(c => c.score).Select(c => c.candidate).ToList();
        LabelCandidates(ordered);

        return ordered.Take(top).ToList();
    }

    private async Task<IEnumerable<ProductCandidateDto>> GatherFromScraper(IProductScraper scraper, ProductQuery query, CancellationToken cancellationToken)
    {
        try
        {
            return await scraper.SearchByQueryAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scraper {Platform} failed", scraper.Platform);
            return Array.Empty<ProductCandidateDto>();
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

    private IEnumerable<(ProductCandidateDto candidate, decimal score)> ScoreCandidates(IEnumerable<ProductCandidateDto> candidates)
    {
        var maxTotal = candidates.Max(c => c.TotalCost);
        var maxShipping = candidates.Max(c => c.ShippingCost);

        foreach (var candidate in candidates)
        {
            var priceScore = maxTotal == 0 ? 0 : 1 - (candidate.TotalCost / maxTotal);
            var ratingScore = (decimal)(candidate.ShopRating / 5.0);
            var shippingScore = maxShipping == 0 ? 1 : 1 - (candidate.ShippingCost / maxShipping);

            var score = priceScore * _options.WeightPrice
                        + ratingScore * _options.WeightRating
                        + shippingScore * _options.WeightShipping;

            yield return (candidate, score);
        }
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

            AssignLabels(candidate, labels);
        }
    }

    private static void AssignLabels(ProductCandidateDto candidate, IEnumerable<string> labels)
    {
        candidate.Labels = labels.Distinct().ToArray();
    }
}

