namespace PriceWatcher.Services.Interfaces;

public interface ISearchJobQueue
{
    ValueTask QueueAsync(SearchJob job, CancellationToken cancellationToken = default);
}

