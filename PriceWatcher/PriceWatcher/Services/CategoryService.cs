using Microsoft.EntityFrameworkCore;
using PriceWatcher.Dtos;
using PriceWatcher.Models;

namespace PriceWatcher.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryByIdAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryDto?> UpdateCategoryAsync(int categoryId, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> AssignProductToCategoryAsync(int productId, int categoryId, CancellationToken cancellationToken = default);
    Task<List<ProductSummaryDto>> GetProductsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<CategoryDto?> AutoCategorizeProduc(int productId, CancellationToken cancellationToken = default);
}

public class CategoryService : ICategoryService
{
    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(PriceWatcherDbContext dbContext, ILogger<CategoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _dbContext.Categories
            .Include(c => c.SubCategories)
            .Where(c => c.ParentCategoryId == null)
            .ToListAsync(cancellationToken);

        return categories.Select(MapToCategoryDto).ToList();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken);

        return category == null ? null : MapToCategoryDto(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = new Category
        {
            CategoryName = dto.CategoryName,
            ParentCategoryId = dto.ParentCategoryId,
            IconUrl = dto.IconUrl,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created category: {CategoryName} (ID: {CategoryId})", category.CategoryName, category.CategoryId);

        return MapToCategoryDto(category);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int categoryId, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories.FindAsync(new object[] { categoryId }, cancellationToken);
        if (category == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dto.CategoryName))
        {
            category.CategoryName = dto.CategoryName;
        }

        if (dto.ParentCategoryId.HasValue)
        {
            category.ParentCategoryId = dto.ParentCategoryId.Value == 0 ? null : dto.ParentCategoryId.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.IconUrl))
        {
            category.IconUrl = dto.IconUrl;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated category ID: {CategoryId}", categoryId);

        return MapToCategoryDto(category);
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories.FindAsync(new object[] { categoryId }, cancellationToken);
        if (category == null)
        {
            return false;
        }

        // Unassign products from this category
        var products = await _dbContext.Products
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.CategoryId = null;
        }

        // Move subcategories to parent or make them root
        var subCategories = await _dbContext.Categories
            .Where(c => c.ParentCategoryId == categoryId)
            .ToListAsync(cancellationToken);

        foreach (var subCategory in subCategories)
        {
            subCategory.ParentCategoryId = category.ParentCategoryId;
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted category ID: {CategoryId}", categoryId);

        return true;
    }

    public async Task<bool> AssignProductToCategoryAsync(int productId, int categoryId, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", productId);
            return false;
        }

        var category = await _dbContext.Categories.FindAsync(new object[] { categoryId }, cancellationToken);
        if (category == null)
        {
            _logger.LogWarning("Category not found: {CategoryId}", categoryId);
            return false;
        }

        product.CategoryId = categoryId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned product {ProductId} to category {CategoryId}", productId, categoryId);

        return true;
    }

    public async Task<List<ProductSummaryDto>> GetProductsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .Include(p => p.Platform)
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId)
            .OrderByDescending(p => p.LastUpdated)
            .ToListAsync(cancellationToken);

        return products.Select(p => new ProductSummaryDto
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            ImageUrl = p.ImageUrl,
            CurrentPrice = p.CurrentPrice ?? 0,
            OriginalPrice = p.OriginalPrice,
            DiscountRate = p.DiscountRate,
            Platform = p.Platform?.PlatformName ?? "Unknown",
            ShopName = p.ShopName,
            Rating = p.Rating,
            CategoryName = p.Category?.CategoryName,
            LastUpdated = p.LastUpdated
        }).ToList();
    }

    public async Task<CategoryDto?> AutoCategorizeProduc(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product == null)
        {
            return null;
        }

        // Simple keyword-based categorization
        var productName = product.ProductName.ToLower();
        var categories = await _dbContext.Categories.ToListAsync(cancellationToken);

        var categoryKeywords = new Dictionary<string, string[]>
        {
            { "Electronics", new[] { "phone", "laptop", "tablet", "computer", "headphone", "speaker", "camera", "tv", "monitor" } },
            { "Fashion", new[] { "shirt", "dress", "pants", "shoes", "jacket", "bag", "watch", "clothing", "fashion" } },
            { "Home & Living", new[] { "furniture", "decor", "kitchen", "bedding", "lamp", "chair", "table", "sofa" } },
            { "Beauty", new[] { "makeup", "skincare", "cosmetic", "perfume", "beauty", "lotion", "cream" } },
            { "Sports", new[] { "sport", "fitness", "gym", "exercise", "yoga", "running", "bike", "outdoor" } },
            { "Books", new[] { "book", "novel", "magazine", "comic", "textbook" } },
            { "Toys", new[] { "toy", "game", "puzzle", "doll", "lego", "kids" } },
            { "Food", new[] { "food", "snack", "drink", "coffee", "tea", "chocolate" } }
        };

        foreach (var kvp in categoryKeywords)
        {
            if (kvp.Value.Any(keyword => productName.Contains(keyword)))
            {
                var category = categories.FirstOrDefault(c => c.CategoryName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                if (category != null)
                {
                    product.CategoryId = category.CategoryId;
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Auto-categorized product {ProductId} to {CategoryName}", productId, category.CategoryName);

                    return MapToCategoryDto(category);
                }
            }
        }

        _logger.LogInformation("Could not auto-categorize product {ProductId}", productId);
        return null;
    }

    private CategoryDto MapToCategoryDto(Category category)
    {
        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            ParentCategoryId = category.ParentCategoryId,
            IconUrl = category.IconUrl,
            CreatedAt = category.CreatedAt,
            SubCategories = category.SubCategories?.Select(MapToCategoryDto).ToList() ?? new List<CategoryDto>()
        };
    }
}
