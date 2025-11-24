using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PriceWatcher.Services
{
    public class CrawlQueueItem
    {
        public string Url { get; set; }
        public int PlatformId { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.Now;
    }

    public interface ICrawlQueue
    {
        void Enqueue(CrawlQueueItem item);
        Task<CrawlQueueItem> DequeueAsync(CancellationToken cancellationToken);
    }

    public class CrawlQueue : ICrawlQueue
    {
        private readonly ConcurrentQueue<CrawlQueueItem> _items = new ConcurrentQueue<CrawlQueueItem>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void Enqueue(CrawlQueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _items.Enqueue(item);
            _signal.Release();
        }

        public async Task<CrawlQueueItem> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _items.TryDequeue(out var item);
            return item;
        }
    }
}
