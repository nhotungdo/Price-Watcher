using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class SearchProcessingWorker : BackgroundService
{
    private readonly ChannelReader<SearchJob> _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SearchProcessingWorker> _logger;

    public SearchProcessingWorker(Channel<SearchJob> channel, IServiceScopeFactory scopeFactory, ILogger<SearchProcessingWorker> logger)
    {
        _reader = channel.Reader;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ISearchProcessingService>();
                await processor.ProcessAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process search job {SearchId}", job.SearchId);
            }
        }
    }
}

