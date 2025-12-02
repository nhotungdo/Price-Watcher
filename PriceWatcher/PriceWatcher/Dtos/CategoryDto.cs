namespace PriceWatcher.Dtos;

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string? IconUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new();
}

public class CreateCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string? IconUrl { get; set; }
}

public class UpdateCategoryDto
{
    public string? CategoryName { get; set; }
    public int? ParentCategoryId { get; set; }
    public string? IconUrl { get; set; }
}

public class ProductSummaryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int? DiscountRate { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? ShopName { get; set; }
    public double? Rating { get; set; }
    public string? CategoryName { get; set; }
    public DateTime? LastUpdated { get; set; }
}
