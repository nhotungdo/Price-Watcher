using System.Collections.Concurrent;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class MetricsService : IMetricsService
{
    private readonly ConcurrentDictionary<string, (long count, long fail, double avg)> _scrapers = new();
    private long _recCount;
    private double _recAvg;

    public void RecordScraperCall(string platform, bool success, long elapsedMs)
    {
        _scrapers.AddOrUpdate(platform,
            _ => (1, success ? 0 : 1, elapsedMs),
            (_, s) =>
            {
                var count = s.count + 1;
                var avg = ((s.avg * s.count) + elapsedMs) / count;
                var fail = s.fail + (success ? 0 : 1);
                return (count, fail, avg);
            });
    }

    public void RecordRecommendation(long elapsedMs)
    {
        var count = Interlocked.Increment(ref _recCount);
        _recAvg = ((_recAvg * (count - 1)) + elapsedMs) / count;
    }

    public MetricsSnapshot GetSnapshot()
    {
        var snap = new MetricsSnapshot
        {
            Recommendations = (int)_recCount,
            RecommendationAvgLatencyMs = _recAvg
        };
        foreach (var kv in _scrapers)
        {
            snap.ScraperCalls[kv.Key] = (int)kv.Value.count;
            snap.ScraperFailures[kv.Key] = (int)kv.Value.fail;
            snap.ScraperAvgLatencyMs[kv.Key] = kv.Value.avg;
        }
        return snap;
    }
}