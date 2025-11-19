namespace PriceWatcher.Services.Interfaces;

public interface IMetricsService
{
    void RecordScraperCall(string platform, bool success, long elapsedMs);
    void RecordRecommendation(long elapsedMs);
    MetricsSnapshot GetSnapshot();
}

public class MetricsSnapshot
{
    public Dictionary<string, int> ScraperCalls { get; set; } = new();
    public Dictionary<string, int> ScraperFailures { get; set; } = new();
    public Dictionary<string, double> ScraperAvgLatencyMs { get; set; } = new();
    public int Recommendations { get; set; }
    public double RecommendationAvgLatencyMs { get; set; }
}