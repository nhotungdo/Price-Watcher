using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Dtos;
using PriceWatcher.Services;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAllCategories(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync(cancellationToken);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return StatusCode(500, new { error = "Failed to retrieve categories" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", id);
            return StatusCode(500, new { error = "Failed to retrieve category" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
            {
                return BadRequest(new { error = "Category name is required" });
            }

            var category = await _categoryService.CreateCategoryAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.CategoryId }, category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, new { error = "Failed to create category" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.UpdateCategoryAsync(id, dto, cancellationToken);
            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, new { error = "Failed to update category" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _categoryService.DeleteCategoryAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound(new { error = "Category not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, new { error = "Failed to delete category" });
        }
    }

    [HttpPost("{categoryId}/products/{productId}")]
    public async Task<ActionResult> AssignProductToCategory(int categoryId, int productId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _categoryService.AssignProductToCategoryAsync(productId, categoryId, cancellationToken);
            if (!result)
            {
                return NotFound(new { error = "Product or category not found" });
            }
            return Ok(new { message = "Product assigned to category successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning product {ProductId} to category {CategoryId}", productId, categoryId);
            return StatusCode(500, new { error = "Failed to assign product to category" });
        }
    }

    [HttpGet("{categoryId}/products")]
    public async Task<ActionResult<List<ProductSummaryDto>>> GetProductsByCategory(int categoryId, CancellationToken cancellationToken)
    {
        try
        {
            var products = await _categoryService.GetProductsByCategoryAsync(categoryId, cancellationToken);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for category {CategoryId}", categoryId);
            return StatusCode(500, new { error = "Failed to retrieve products" });
        }
    }

    [HttpPost("auto-categorize/{productId}")]
    public async Task<ActionResult<CategoryDto>> AutoCategorizeProduct(int productId, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.AutoCategorizeProduc(productId, cancellationToken);
            if (category == null)
            {
                return Ok(new { message = "Could not auto-categorize product", category = (CategoryDto?)null });
            }
            return Ok(new { message = "Product auto-categorized successfully", category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-categorizing product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to auto-categorize product" });
        }
    }
}
