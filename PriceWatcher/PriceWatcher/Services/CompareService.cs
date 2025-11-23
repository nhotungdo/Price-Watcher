using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public interface ICompareService
{
    Task<CompareResultDto> CompareByUrlAsync(string sourceUrl, CancellationToken cancellationToken = default);
}

public class CompareService : ICompareService
{
    private readonly ILinkProcessor _linkProcessor;
    private readonly IEnumerable<IProductScraper> _scrapers;
    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILogger<CompareService> _logger;

    public CompareService(
        ILinkProcessor linkProcessor,
        IEnumerable<IProductScraper> scrapers,
        PriceWatcherDbContext dbContext,
        ILogger<CompareService> logger)
    {
        _linkProcessor = linkProcessor;
        _scrapers = scrapers;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CompareResultDto> CompareByUrlAsync(string sourceUrl, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        // 1. Detect platform & product id
        var query = await _linkProcessor.ProcessUrlAsync(sourceUrl, cancellationToken);
        if (query == null)
        {
            throw new ArgumentException("Unsupported or invalid URL", nameof(sourceUrl));
        }

        // 2. Get source product details via platform‑specific scraper
        var sourceScraper = _scrapers.FirstOrDefault(s => string.Equals(s.Platform, query.Platform, StringComparison.OrdinalIgnoreCase));
        if (sourceScraper == null)
        {
            throw new InvalidOperationException($"No scraper registered for platform '{query.Platform}'");
        }
        var sourceCandidate = await sourceScraper.GetByUrlAsync(query, cancellationToken);
        if (sourceCandidate == null)
        {
            throw new InvalidOperationException($"Unable to retrieve product details for {sourceUrl}");
        }

        // 3. Build a search query (prefer SKU if present, otherwise title)
        var searchTitle = !string.IsNullOrWhiteSpace(sourceCandidate.Title) ? sourceCandidate.Title : query.TitleHint;
        var searchOptions = new SearchOptions { Platforms = new[] { "shopee", "lazada", "tiki" } };

        // 4. Search each platform in parallel (rate‑limited inside clients)
        var tasks = _scrapers.Select(async scraper =>
        {
            try
            {
                var q = new ProductQuery { Platform = scraper.Platform, ProductId = query.ProductId, CanonicalUrl = query.CanonicalUrl, TitleHint = searchTitle };
                return await scraper.SearchByQueryAsync(q, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Search failed on {Platform}", scraper.Platform);
                warnings.Add($"Search failed on {scraper.Platform}: {ex.Message}");
                return Enumerable.Empty<ProductCandidateDto>();
            }
        });
        var results = await Task.WhenAll(tasks);
        var candidates = results.SelectMany(x => x).ToList();

        // 5. Compute match scores and price normalisation
        var candidateDtos = new List<CandidateDto>();
        foreach (var cand in candidates)
        {
            var shippingVnd = cand.ShippingCost;
            var total = cand.Price + shippingVnd;
            var matchScore = CalculateTitleSimilarity(sourceCandidate.Title, cand.Title);
            var reasons = BuildMatchReasons(sourceCandidate.Title, cand.Title, matchScore);
            candidateDtos.Add(new CandidateDto
            {
                Product = new ProductDto
                {
                    Platform = cand.Platform,
                    ProductId = (cand.ProductId?.ToString() ?? string.Empty),
                    Title = cand.Title,
                    PriceRaw = cand.Price,
                    Currency = "VND",
                    Images = string.IsNullOrWhiteSpace(cand.ThumbnailUrl) ? new List<string>() : new List<string> { cand.ThumbnailUrl },
                    Url = cand.ProductUrl
                },
                ShippingEstimate = shippingVnd,
                TotalPriceNormalized = total,
                MatchScore = matchScore,
                MatchReasons = reasons
            });
        }

        // 6. Rank candidates and pick best price
        var best = candidateDtos.OrderByDescending(c => c.MatchScore).ThenBy(c => c.TotalPriceNormalized).FirstOrDefault();
        var bestDto = best == null ? new BestPriceDto() : new BestPriceDto
        {
            Platform = best.Product.Platform,
            Url = best.Product.Url,
            TotalPriceVnd = best.TotalPriceNormalized,
            Confidence = best.MatchScore
        };

        // 7. Persist mapping for future caching
        var mapping = new ProductMapping
        {
            SourceUrl = sourceUrl,
            SourceProductId = sourceCandidate.ProductId?.ToString() ?? string.Empty,
            MatchedCandidatesJson = System.Text.Json.JsonSerializer.Serialize(candidateDtos),
            LastSeen = DateTime.UtcNow
        };
        _dbContext.ProductMappings.Add(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CompareResultDto
        {
            SourceProduct = new ProductDto
            {
                Platform = sourceScraper.Platform,
                ProductId = sourceCandidate.ProductId?.ToString() ?? string.Empty,
                Title = sourceCandidate.Title,
                PriceRaw = sourceCandidate.Price,
                Currency = "VND",
                Images = string.IsNullOrWhiteSpace(sourceCandidate.ThumbnailUrl) ? new List<string>() : new List<string> { sourceCandidate.ThumbnailUrl },
                Url = query.CanonicalUrl
            },
            Candidates = candidateDtos,
            BestPrice = bestDto,
            Warnings = warnings
        };
    }

    private static double CalculateTitleSimilarity(string? a, string? b)
    {
        var ta = Tokenize(a);
        var tb = Tokenize(b);
        if (ta.Count == 0 || tb.Count == 0) return 0;
        var inter = ta.Intersect(tb).Count();
        var union = ta.Union(tb).Count();
        return union == 0 ? 0 : (double)inter / union;
    }

    private static List<string> BuildMatchReasons(string? a, string? b, double score)
    {
        var reasons = new List<string>();
        if (score >= 0.6) reasons.Add("Tựa đề tương đồng");
        if (!string.IsNullOrWhiteSpace(b) && b.Contains("LazMall", StringComparison.OrdinalIgnoreCase)) reasons.Add("Shop chính hãng/LazMall");
        return reasons;
    }

    private static HashSet<string> Tokenize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parts = s.ToLowerInvariant()
            .Replace("-", " ")
            .Replace("_", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 1 && !char.IsDigit(x[0]));
        return new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
    }
}
