using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PriceWatcher.Pages;

public class MultiSearchModel : PageModel
{
    public string? Keyword { get; set; }

    public void OnGet(string? keyword)
    {
        Keyword = keyword;
    }
}
