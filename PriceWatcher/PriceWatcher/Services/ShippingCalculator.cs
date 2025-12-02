using Microsoft.EntityFrameworkCore;
using PriceWatcher.Models;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class ShippingCalculator : IShippingCalculator
{
    private readonly PriceWatcherDbContext _dbContext;

    public ShippingCalculator(PriceWatcherDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> CalculateShippingInfoAsync(int productId, int platformId, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .Include(p => p.Platform)
            .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

        if (product == null)
            return "Shipping info unavailable";

        // Check if product has free shipping
        if (product.IsFreeShip == true)
            return "Free shipping";

        // Platform-specific shipping rules
        var platformName = product.Platform?.PlatformName?.ToLower();
        var price = product.CurrentPrice ?? 0;

        return platformName switch
        {
            "shopee" => price >= 50000 ? "Free shipping" : "Shipping: 15,000₫",
            "lazada" => price >= 100000 ? "Free shipping" : "Shipping: 20,000₫",
            "tiki" => price >= 150000 ? "Free shipping (TikiNOW)" : "Shipping: 25,000₫",
            _ => product.ShippingInfo ?? "Standard shipping"
        };
    }

    public decimal CalculateShippingCost(int platformId, decimal productPrice, string? shippingInfo = null)
    {
        // If shipping info indicates free shipping
        if (shippingInfo?.Contains("Free", StringComparison.OrdinalIgnoreCase) == true)
            return 0;

        // Platform-specific thresholds
        return platformId switch
        {
            1 => productPrice >= 50000 ? 0 : 15000,   // Shopee
            2 => productPrice >= 100000 ? 0 : 20000,  // Lazada
            3 => productPrice >= 150000 ? 0 : 25000,  // Tiki
            _ => 20000 // Default shipping cost
        };
    }
}
