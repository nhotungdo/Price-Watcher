namespace PriceWatcher.Dtos;

public class SearchStatusDto
{
    public Guid SearchId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Message { get; set; }
    public IReadOnlyCollection<ProductCandidateDto>? Results { get; set; }
}

