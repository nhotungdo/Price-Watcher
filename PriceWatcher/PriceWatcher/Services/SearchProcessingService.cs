using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class SearchProcessingService : ISearchProcessingService
{
    private readonly ILinkProcessor _linkProcessor;
    private readonly IImageSearchService _imageSearchService;
    private readonly IRecommendationService _recommendationService;
    private readonly ISearchHistoryService _searchHistoryService;
    private readonly ISearchStatusService _statusService;
    private readonly ILogger<SearchProcessingService> _logger;

    public SearchProcessingService(
        ILinkProcessor linkProcessor,
        IImageSearchService imageSearchService,
        IRecommendationService recommendationService,
        ISearchHistoryService searchHistoryService,
        ISearchStatusService statusService,
        ILogger<SearchProcessingService> logger)
    {
        _linkProcessor = linkProcessor;
        _imageSearchService = imageSearchService;
        _recommendationService = recommendationService;
        _searchHistoryService = searchHistoryService;
        _statusService = statusService;
        _logger = logger;
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
                _logger.LogWarning(ex, "Cannot extract product id for {SearchId}", job.SearchId);
                _statusService.Fail(job.SearchId, "Không xác định được mã sản phẩm từ URL. Vui lòng dùng link chi tiết sản phẩm.");
                return;
            }

            if (query == null)
            {
                _statusService.Fail(job.SearchId, "Unable to resolve query from input.");
                return;
            }

            var recommendations = (await _recommendationService.RecommendAsync(query, cancellationToken: cancellationToken)).ToArray();

            await _searchHistoryService.SaveSearchHistoryAsync(
                job.SearchId,
                job.UserId,
                job.SearchType,
                job.Url ?? job.QueryOverride?.CanonicalUrl ?? "image",
                query,
                recommendations,
                cancellationToken);

            _statusService.Complete(job.SearchId, recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search processing failed for {SearchId}", job.SearchId);
            _statusService.Fail(job.SearchId, "Processing failed. Please try again later.");
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
}

