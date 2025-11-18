namespace PriceWatcher.Services.Interfaces;

public interface ISearchProcessingService
{
    Task ProcessAsync(SearchJob job, CancellationToken cancellationToken);
}

