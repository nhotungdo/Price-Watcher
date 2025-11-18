using System.Collections.Generic;

namespace PriceWatcher.Dtos;

public class ProductQuery
{
    public string Platform { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string? TitleHint { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
}

