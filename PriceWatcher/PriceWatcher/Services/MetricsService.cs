using PriceWatcher.Services.Interfaces;
using System.Collections.Concurrent;

namespace PriceWatcher.Services;

public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly ConcurrentDictionary<string, MetricData> _metrics = new();

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
    }

    public void RecordScraperCall(string scraperName, bool success, long elapsedMs)
    {
        _logger.LogInformation(
            "Scraper call: {ScraperName}, Success: {Success}, Duration: {ElapsedMs}ms",
            scraperName, success, elapsedMs);

        var key = $"scraper_{scraperName}";
        _metrics.AddOrUpdate(key,
            new MetricData { TotalCalls = 1, SuccessfulCalls = success ? 1 : 0, TotalDurationMs = elapsedMs },
            (_, existing) =>
            {
                existing.TotalCalls++;
                if (success) existing.SuccessfulCalls++;
                existing.TotalDurationMs += elapsedMs;
                return existing;
            });
    }

    public object GetSnapshot()
    {
        return new
        {
            timestamp = DateTime.UtcNow,
            metrics = _metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    totalCalls = kvp.Value.TotalCalls,
                    successfulCalls = kvp.Value.SuccessfulCalls,
                    failedCalls = kvp.Value.TotalCalls - kvp.Value.SuccessfulCalls,
                    averageDurationMs = kvp.Value.TotalCalls > 0 ? kvp.Value.TotalDurationMs / kvp.Value.TotalCalls : 0
                })
        };
    }

    private class MetricData
    {
        public long TotalCalls { get; set; }
        public long SuccessfulCalls { get; set; }
        public long TotalDurationMs { get; set; }
    }
}
