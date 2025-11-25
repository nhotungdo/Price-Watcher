using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Pages;

public class SearchModel : PageModel
{
    private readonly IProductSearchService _productSearchService;
    private readonly ILogger<SearchModel> _logger;

    public SearchModel(IProductSearchService productSearchService, ILogger<SearchModel> logger)
    {
        _productSearchService = productSearchService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    public new ProductSearchResponse Response { get; private set; } = ProductSearchResponse.Empty();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            Response = ProductSearchResponse.Empty();
            return Page();
        }

        try
        {
            var sanitizedPage = PageNumber <= 0 ? 1 : PageNumber;
            Response = await _productSearchService.SearchAsync(Query, sanitizedPage, 12, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search page query failed for {Query}", Query);
            Response = ProductSearchResponse.Empty(Query) with
            {
                Notices = new[]
                {
                    new SearchNotification("error", "Đã xảy ra lỗi khi tìm kiếm. Vui lòng thử lại sau.")
                }
            };
        }

        return Page();
    }
}

