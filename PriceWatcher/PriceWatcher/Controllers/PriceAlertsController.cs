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
    [Route("api/users/{userId}/alerts")]
    [Authorize]
    public class PriceAlertsController : ControllerBase
    {
        private readonly PriceWatcherDbContext _context;
        private readonly ILogger<PriceAlertsController> _logger;

        public PriceAlertsController(PriceWatcherDbContext context, ILogger<PriceAlertsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all price alerts for a user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<PriceAlertDto>>> GetAlerts(int userId)
        {
            // Verify user authorization
            if (!IsAuthorizedUser(userId))
            {
                return Forbid();
            }

            var alerts = await _context.PriceAlerts
                .Include(a => a.Product)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new PriceAlertDto
                {
                    AlertId = a.AlertId,
                    UserId = a.UserId,
                    ProductId = a.ProductId,
                    ProductName = a.Product.ProductName,
                    ProductImageUrl = a.Product.ImageUrl,
                    TargetPrice = a.TargetPrice,
                    CurrentPrice = a.Product.CurrentPrice,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt.Value,
                    LastNotifiedAt = a.LastNotifiedAt
                })
                .ToListAsync();

            return Ok(alerts);
        }

        /// <summary>
        /// Create a new price alert
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PriceAlertDto>> CreateAlert(int userId, [FromBody] CreatePriceAlertDto dto)
        {
            // Verify user authorization
            if (!IsAuthorizedUser(userId))
            {
                return Forbid();
            }

            // Verify product exists
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            // Check if alert already exists
            var existingAlert = await _context.PriceAlerts
                .FirstOrDefaultAsync(a => a.UserId == userId && 
                                         a.ProductId == dto.ProductId && 
                                         a.IsActive);

            if (existingAlert != null)
            {
                return BadRequest(new { message = "An active alert already exists for this product" });
            }

            var alert = new PriceAlert
            {
                UserId = userId,
                ProductId = dto.ProductId,
                TargetPrice = dto.TargetPrice,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.PriceAlerts.Add(alert);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created price alert {AlertId} for user {UserId}", alert.AlertId, userId);

            var resultDto = new PriceAlertDto
            {
                AlertId = alert.AlertId,
                UserId = alert.UserId,
                ProductId = alert.ProductId,
                ProductName = product.ProductName,
                ProductImageUrl = product.ImageUrl,
                TargetPrice = alert.TargetPrice,
                CurrentPrice = product.CurrentPrice,
                IsActive = alert.IsActive,
                CreatedAt = alert.CreatedAt.Value,
                LastNotifiedAt = alert.LastNotifiedAt
            };

            return CreatedAtAction(nameof(GetAlerts), new { userId }, resultDto);
        }

        /// <summary>
        /// Delete a price alert
        /// </summary>
        [HttpDelete("{alertId}")]
        public async Task<IActionResult> DeleteAlert(int userId, int alertId)
        {
            // Verify user authorization
            if (!IsAuthorizedUser(userId))
            {
                return Forbid();
            }

            var alert = await _context.PriceAlerts
                .FirstOrDefaultAsync(a => a.AlertId == alertId && a.UserId == userId);

            if (alert == null)
            {
                return NotFound();
            }

            _context.PriceAlerts.Remove(alert);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted price alert {AlertId} for user {UserId}", alertId, userId);

            return NoContent();
        }

        /// <summary>
        /// Update alert status (activate/deactivate)
        /// </summary>
        [HttpPatch("{alertId}")]
        public async Task<IActionResult> UpdateAlertStatus(int userId, int alertId, [FromBody] bool isActive)
        {
            // Verify user authorization
            if (!IsAuthorizedUser(userId))
            {
                return Forbid();
            }

            var alert = await _context.PriceAlerts
                .FirstOrDefaultAsync(a => a.AlertId == alertId && a.UserId == userId);

            if (alert == null)
            {
                return NotFound();
            }

            alert.IsActive = isActive;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool IsAuthorizedUser(int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null && int.TryParse(userIdClaim, out var claimUserId) && claimUserId == userId;
        }
    }
}
