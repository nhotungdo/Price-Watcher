using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;
using PriceWatcher.Dtos;

namespace PriceWatcher.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly PriceWatcherDbContext _context;

        public CategoriesController(PriceWatcherDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all categories as a tree structure
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<CategoryDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ParentCategoryId = c.ParentCategoryId,
                    IconUrl = c.IconUrl,
                    SubCategories = c.SubCategories.Select(sc => new CategoryDto
                    {
                        CategoryId = sc.CategoryId,
                        CategoryName = sc.CategoryName,
                        ParentCategoryId = sc.ParentCategoryId,
                        IconUrl = sc.IconUrl,
                        SubCategories = new List<CategoryDto>()
                    }).ToList()
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Get a specific category by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ParentCategoryId = c.ParentCategoryId,
                    IconUrl = c.IconUrl,
                    SubCategories = c.SubCategories.Select(sc => new CategoryDto
                    {
                        CategoryId = sc.CategoryId,
                        CategoryName = sc.CategoryName,
                        ParentCategoryId = sc.ParentCategoryId,
                        IconUrl = sc.IconUrl,
                        SubCategories = new List<CategoryDto>()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }
    }
}
