using System.ComponentModel.DataAnnotations;

namespace PriceWatcher.Dtos;

public class UpdateCartItemRequest
{
    [Range(1, 99)]
    public int Quantity { get; set; }
}

