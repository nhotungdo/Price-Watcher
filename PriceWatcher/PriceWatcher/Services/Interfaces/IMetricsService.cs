namespace PriceWatcher.Services.Interfaces;

public interface IMetricsService
{
    void RecordScraperCall(string scraperName, bool success, long elapsedMs);
    object GetSnapshot();
}
