using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceWatcher.Models;
using PriceWatcher.Dtos;

namespace PriceWatcher.Controllers
{
    [ApiController]
    [Route("api/products/{productId}/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly PriceWatcherDbContext _context;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(PriceWatcherDbContext context, ILogger<ReviewsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get reviews for a product
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ReviewDto>>> GetReviews(
            int productId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewDto
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductId,
                    UserId = r.UserId,
                    UserName = r.User.FullName ?? "Anonymous",
                    UserAvatarUrl = r.User.AvatarUrl,
                    Stars = r.Stars,
                    Content = r.Content,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    CreatedAt = r.CreatedAt.Value
                })
                .ToListAsync();

            var totalCount = await _context.Reviews.CountAsync(r => r.ProductId == productId);

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(reviews);
        }

        /// <summary>
        /// Create a new review
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReviewDto>> CreateReview(int productId, [FromBody] CreateReviewDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Verify product exists
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            // Check if user already reviewed this product
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingReview != null)
            {
                return BadRequest(new { message = "You have already reviewed this product" });
            }

            // Validate stars
            if (dto.Stars < 1 || dto.Stars > 5)
            {
                return BadRequest(new { message = "Stars must be between 1 and 5" });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Stars = dto.Stars,
                Content = dto.Content,
                IsVerifiedPurchase = false, // TODO: Implement purchase verification
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created review {ReviewId} for product {ProductId} by user {UserId}", 
                review.ReviewId, productId, userId);

            var user = await _context.Users.FindAsync(userId);
            var resultDto = new ReviewDto
            {
                ReviewId = review.ReviewId,
                ProductId = review.ProductId,
                UserId = review.UserId,
                UserName = user?.FullName ?? "Anonymous",
                UserAvatarUrl = user?.AvatarUrl,
                Stars = review.Stars,
                Content = review.Content,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                CreatedAt = review.CreatedAt.Value
            };

            return CreatedAtAction(nameof(GetReviews), new { productId }, resultDto);
        }

        /// <summary>
        /// Delete a review (only by the author)
        /// </summary>
        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int productId, int reviewId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.ProductId == productId);

            if (review == null)
            {
                return NotFound();
            }

            if (review.UserId != userId)
            {
                return Forbid();
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted review {ReviewId} for product {ProductId}", reviewId, productId);

            return NoContent();
        }
    }
}
