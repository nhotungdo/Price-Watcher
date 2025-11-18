using System.ComponentModel.DataAnnotations;

namespace PriceWatcher.Dtos;

public class SearchSubmitRequest
{
    [Required]
    public int UserId { get; set; }

    public string? Url { get; set; }
}

