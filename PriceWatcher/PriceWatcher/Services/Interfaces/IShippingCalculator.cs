namespace PriceWatcher.Services.Interfaces;

public interface IShippingCalculator
{
    Task<string> CalculateShippingInfoAsync(int productId, int platformId, CancellationToken cancellationToken = default);
    decimal CalculateShippingCost(int platformId, decimal productPrice, string? shippingInfo = null);
}
