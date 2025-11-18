namespace PriceWatcher.Dtos;

public class SearchHistoryDto
{
    public int HistoryId { get; set; }
    public string? SearchType { get; set; }
    public string? InputContent { get; set; }
    public string? DetectedKeyword { get; set; }
    public decimal? BestPriceFound { get; set; }
    public DateTime? SearchTime { get; set; }
}

