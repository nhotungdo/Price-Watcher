using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PriceWatcher.Pages;

public class TrackingModel : PageModel
{
    public int? CurrentUserId { get; private set; }

    public void OnGet()
    {
        var id = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(id, out var uid))
        {
            CurrentUserId = uid;
        }
    }
}