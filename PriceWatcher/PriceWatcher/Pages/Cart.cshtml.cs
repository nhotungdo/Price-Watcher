using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PriceWatcher.Pages
{
    public class CartModel : PageModel
    {
        public void OnGet()
        {
            // Cart data is managed client-side via localStorage
            // This page just renders the UI
        }
    }
}
