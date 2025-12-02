using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PriceWatcher.Pages
{
    public class CategoryModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string CategoryName { get; set; } = string.Empty;

        public string CategoryIcon { get; set; } = "📦";
        public int ProductCount { get; set; } = 0;

        public void OnGet()
        {
            // Map category names to icons
            CategoryIcon = CategoryName?.ToLower() switch
            {
                "electronics" or "điện thoại" or "điện thoại - mtb" => "📱",
                "laptop" or "laptop - it" => "💻",
                "camera" or "máy ảnh" => "📷",
                "audio" or "âm thanh" => "🎧",
                "watch" or "đồng hồ" => "⌚",
                "home" or "nhà cửa" => "🏠",
                "books" or "sách" => "📚",
                "fashion" or "thời trang" => "👕",
                _ => "📦"
            };
        }
    }
}
