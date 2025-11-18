using System.Threading.Channels;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class SearchJobQueue : ISearchJobQueue
{
    private readonly ChannelWriter<SearchJob> _writer;

    public SearchJobQueue(Channel<SearchJob> channel)
    {
        _writer = channel.Writer;
    }

    public async ValueTask QueueAsync(SearchJob job, CancellationToken cancellationToken = default)
    {
        await _writer.WriteAsync(job, cancellationToken);
    }
}

