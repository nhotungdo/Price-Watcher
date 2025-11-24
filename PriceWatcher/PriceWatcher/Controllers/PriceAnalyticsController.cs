using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;
using PriceWatcher.Services;

namespace PriceWatcher.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class PriceAnalyticsController : ControllerBase
    {
        private readonly IPriceAnalyticsService _analyticsService;
        private readonly ILogger<PriceAnalyticsController> _logger;

        public PriceAnalyticsController(
            IPriceAnalyticsService analyticsService,
            ILogger<PriceAnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Get price history for a product
        /// </summary>
        [HttpGet("{productId}/price-history")]
        public async Task<ActionResult<PriceHistoryResponseDto>> GetPriceHistory(
            int productId,
            [FromQuery] int days = 90,
            [FromQuery] string granularity = "daily")
        {
            try
            {
                var history = await _analyticsService.GetPriceHistoryAsync(productId, days, granularity);

                if (history == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                if (history.History.Count < 7)
                {
                    return Ok(new
                    {
                        productId = history.ProductId,
                        productName = history.ProductName,
                        message = "Insufficient data for analysis",
                        dataPoints = history.History.Count
                    });
                }

                return Ok(history);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving price history for product {ProductId}", productId);
                return StatusCode(500, new { message = "An error occurred while retrieving price history" });
            }
        }

        /// <summary>
        /// Get buy signal recommendation for a product
        /// </summary>
        [HttpGet("{productId}/buy-signal")]
        public async Task<ActionResult<BuySignalDto>> GetBuySignal(
            int productId,
            [FromQuery] int horizon = 7)
        {
            try
            {
                var signal = await _analyticsService.GetBuySignalAsync(productId, horizon);

                if (signal == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                return Ok(signal);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving buy signal for product {ProductId}", productId);
                return StatusCode(500, new { message = "An error occurred while retrieving buy signal" });
            }
        }
    }
}
