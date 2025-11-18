using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PriceWatcher.Dtos;

public class SearchSubmitForm
{
    [Required]
    public int UserId { get; set; }

    public string? Url { get; set; }

    public IFormFile? Image { get; set; }
}

