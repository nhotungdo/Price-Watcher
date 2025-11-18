using System.Collections.Concurrent;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class SearchStatusService : ISearchStatusService
{
    private readonly ConcurrentDictionary<Guid, SearchStatusDto> _statuses = new();

    public void Initialize(Guid searchId)
    {
        _statuses[searchId] = new SearchStatusDto
        {
            SearchId = searchId,
            Status = "Pending",
            Message = "Waiting for processing"
        };
    }

    public void MarkProcessing(Guid searchId)
    {
        _statuses.AddOrUpdate(searchId,
            id => new SearchStatusDto { SearchId = id, Status = "Processing" },
            (_, existing) =>
            {
                existing.Status = "Processing";
                existing.Message = "Processing";
                return existing;
            });
    }

    public void Complete(Guid searchId, IEnumerable<ProductCandidateDto> results)
    {
        var dto = new SearchStatusDto
        {
            SearchId = searchId,
            Status = "Completed",
            Results = results.ToArray()
        };
        _statuses[searchId] = dto;
    }

    public void Fail(Guid searchId, string message)
    {
        _statuses[searchId] = new SearchStatusDto
        {
            SearchId = searchId,
            Status = "Failed",
            Message = message
        };
    }

    public SearchStatusDto? GetStatus(Guid searchId)
    {
        _statuses.TryGetValue(searchId, out var status);
        return status;
    }
}

