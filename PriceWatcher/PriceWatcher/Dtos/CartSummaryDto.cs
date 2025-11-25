namespace PriceWatcher.Dtos;

public class CartSummaryDto
{
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

