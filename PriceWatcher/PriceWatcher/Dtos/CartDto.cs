namespace PriceWatcher.Dtos;

public class CartDto
{
    public IReadOnlyList<CartItemDto> Items { get; set; } = Array.Empty<CartItemDto>();
    public CartSummaryDto Summary { get; set; } = new CartSummaryDto();
    public bool IsAuthenticatedCart { get; set; }
}

