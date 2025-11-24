using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using PriceWatcher.Services;

namespace PriceWatcher.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductComparisonController : ControllerBase
    {
        private readonly IProductComparisonService _comparisonService;
        private readonly ILogger<ProductComparisonController> _logger;

        public ProductComparisonController(
            IProductComparisonService comparisonService,
            ILogger<ProductComparisonController> logger)
        {
            _comparisonService = comparisonService;
            _logger = logger;
        }

        /// <summary>
        /// Get price comparisons for a product mapping across all platforms
        /// </summary>
        [HttpGet("{mappingId}/comparisons")]
        public async Task<ActionResult<List<ProductComparisonDto>>> GetComparisons(
            int mappingId,
            [FromQuery] string currency = "VND",
            [FromQuery] string sort = "price",
            [FromQuery] bool onlyAvailable = false)
        {
            try
            {
                var comparisons = await _comparisonService.GetProductComparisonsAsync(
                    mappingId, currency, sort, onlyAvailable);

                if (comparisons == null || comparisons.Count == 0)
                {
                    return NotFound(new { message = "No products found for this mapping" });
                }

                _logger.LogInformation("Retrieved {Count} comparisons for mapping {MappingId}", 
                    comparisons.Count, mappingId);

                return Ok(comparisons);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comparisons for mapping {MappingId}", mappingId);
                return StatusCode(500, new { message = "An error occurred while retrieving comparisons" });
            }
        }

        /// <summary>
        /// Get price comparisons by product ID
        /// </summary>
        [HttpGet("compare/{productId}")]
        public async Task<ActionResult<List<ProductComparisonDto>>> GetComparisonsByProductId(int productId)
        {
            try
            {
                var comparisons = await _comparisonService.GetProductComparisonsByProductIdAsync(productId);

                if (comparisons == null || comparisons.Count == 0)
                {
                    return NotFound(new { message = "No similar products found" });
                }

                return Ok(comparisons);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comparisons for product {ProductId}", productId);
                return StatusCode(500, new { message = "An error occurred while retrieving comparisons" });
            }
        }
    }
}
