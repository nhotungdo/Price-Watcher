using System.Collections.Generic;
using System.Collections.Generic;

namespace PriceWatcher.Dtos
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? ParentCategoryId { get; set; }
        public int ProductCount { get; set; }
        public string? IconUrl { get; set; }
        public List<CategoryDto> SubCategories { get; set; }
    }
}
