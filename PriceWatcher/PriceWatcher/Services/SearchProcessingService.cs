using Microsoft.Extensions.Logging;
using System.Text;
using System.Net.Http;
using PriceWatcher.Services.Interfaces;
using PriceWatcher.Dtos;

namespace PriceWatcher.Services;

public class SearchProcessingService : ISearchProcessingService
{
    private readonly ILinkProcessor _linkProcessor;
    private readonly IImageSearchService _imageSearchService;
    private readonly IRecommendationService _recommendationService;
    private readonly ISearchHistoryService _searchHistoryService;
    private readonly ISearchStatusService _statusService;
    private readonly ILogger<SearchProcessingService> _logger;
    private readonly IImageEmbeddingService _embeddingService;
    private readonly IHttpClientFactory _httpClientFactory;

    public SearchProcessingService(
        ILinkProcessor linkProcessor,
        IImageSearchService imageSearchService,
        IRecommendationService recommendationService,
        ISearchHistoryService searchHistoryService,
        ISearchStatusService statusService,
        ILogger<SearchProcessingService> logger,
        IImageEmbeddingService embeddingService,
        IHttpClientFactory httpClientFactory)
    {
        _linkProcessor = linkProcessor;
        _imageSearchService = imageSearchService;
        _recommendationService = recommendationService;
        _searchHistoryService = searchHistoryService;
        _statusService = statusService;
        _logger = logger;
        _embeddingService = embeddingService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task ProcessAsync(SearchJob job, CancellationToken cancellationToken)
    {
        _statusService.MarkProcessing(job.SearchId);

        try
        {
            ProductQuery? query;
            try
            {
                query = job.QueryOverride ?? await ResolveQueryAsync(job, cancellationToken);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input URL for {SearchId}", job.SearchId);
                _statusService.Fail(job.SearchId, "URL sản phẩm không hợp lệ.");
                return;
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning(ex, "Unsupported platform for {SearchId}", job.SearchId);
                _statusService.Fail(job.SearchId, "Nền tảng chưa được hỗ trợ (chỉ Shopee/Lazada/Tiki).");
                return;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Query resolve failed for {SearchId}", job.SearchId);
                var msg = ex.Message;
                if (string.IsNullOrWhiteSpace(msg)) msg = "Không xác định được truy vấn từ dữ liệu đầu vào.";
                _statusService.Fail(job.SearchId, msg);
                return;
            }

            if (query == null)
            {
                _statusService.Fail(job.SearchId, "Unable to resolve query from input.");
                return;
            }

            var recommendations = (await _recommendationService.RecommendAsync(query, top: 12, cancellationToken: cancellationToken)).ToArray();

            if (job.SearchType == "image" && job.ImageBytes is { Length: > 0 })
            {
                try
                {
                    await using var imgStream = new MemoryStream(job.ImageBytes);
                    var sourceEmb = await _embeddingService.ComputeEmbeddingAsync(imgStream, cancellationToken);
                    var validated = new List<ProductCandidateDto>();
                    foreach (var c in recommendations)
                    {
                        if (string.IsNullOrWhiteSpace(c.ThumbnailUrl))
                        {
                            validated.Add(c);
                            continue;
                        }
                        try
                        {
                            await using var thumbStream = await DownloadImageAsync(c.ThumbnailUrl, cancellationToken);
                            var thumbEmb = await _embeddingService.ComputeEmbeddingAsync(thumbStream, cancellationToken);
                            var sim = _embeddingService.CosineSimilarity(sourceEmb, thumbEmb);
                            c.ImageSimilarity = sim;
                            c.IsImageMatch = sim >= 0.45;
                            if (c.IsImageMatch == true) validated.Add(c);
                            else _logger.LogInformation("Filtered candidate due to low image similarity: {Title} ({Platform}) sim={Sim:F2}", c.Title, c.Platform, sim);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Thumbnail validate failed for {Url}", c.ThumbnailUrl);
                            validated.Add(c);
                        }
                    }

                    if (validated.Count == 0)
                    {
                        var tokenized = Tokenize(query.TitleHint ?? string.Empty);
                        var rescored = recommendations
                            .Select(c => new { C = c, Score = TextScore(c.Title ?? string.Empty, tokenized) })
                            .OrderByDescending(x => x.Score)
                            .Take(12)
                            .Select(x => { x.C.FitReason = $"Text match: {x.Score:P0}"; return x.C; })
                            .ToArray();
                        _logger.LogInformation("No image matches; using text match fallback");
                        recommendations = rescored;
                    }
                    else
                    {
                        recommendations = validated.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Image validation step failed; proceed without filtering");
                }
            }

            await _searchHistoryService.SaveSearchHistoryAsync(
                job.SearchId,
                job.UserId,
                job.SearchType,
                job.Url ?? job.QueryOverride?.CanonicalUrl ?? "image",
                query,
                recommendations,
                cancellationToken);

            _statusService.Complete(job.SearchId, query, recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search processing failed for {SearchId}", job.SearchId);
            var msg = ex.Message;
            if (string.IsNullOrWhiteSpace(msg)) msg = "Processing failed. Please try again later.";
            _statusService.Fail(job.SearchId, msg);
        }
    }

    private async Task<ProductQuery?> ResolveQueryAsync(SearchJob job, CancellationToken cancellationToken)
    {
        if (job.SearchType == "url" && !string.IsNullOrWhiteSpace(job.Url))
        {
            return await _linkProcessor.ProcessUrlAsync(job.Url, cancellationToken);
        }

        if (job.SearchType == "image" && job.ImageBytes is { Length: > 0 })
        {
            await using var stream = new MemoryStream(job.ImageBytes);
            var queries = await _imageSearchService.SearchByImageAsync(stream, cancellationToken);
            return queries.FirstOrDefault();
        }

        return null;
    }

    private async Task<Stream> DownloadImageAsync(string url, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
        var res = await client.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        var ms = new MemoryStream();
        await res.Content.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    private static string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        text = RemoveDiacritics(text.ToLowerInvariant());
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\p{L}\s]", " ");
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static double TextScore(string title, string[] tokens)
    {
        if (tokens.Length == 0 || string.IsNullOrWhiteSpace(title)) return 0;
        var t = new HashSet<string>(Tokenize(title));
        if (t.Count == 0) return 0;
        var inter = tokens.Count(tok => t.Contains(tok));
        var union = t.Count + tokens.Length - inter;
        return union == 0 ? 0 : (double)inter / union;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

