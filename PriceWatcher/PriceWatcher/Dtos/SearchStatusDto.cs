namespace PriceWatcher.Dtos;

public class SearchStatusDto
{
    public Guid SearchId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Message { get; set; }
    public IReadOnlyCollection<ProductCandidateDto>? Results { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? ProductName { get; set; }
    public string? ProductImageUrl { get; set; }
    public string? Category { get; set; }
    public IReadOnlyCollection<ProductCandidateDto>? Lower { get; set; }
    public IReadOnlyCollection<ProductCandidateDto>? Higher { get; set; }
}

