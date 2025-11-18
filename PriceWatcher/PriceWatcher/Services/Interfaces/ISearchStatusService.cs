using PriceWatcher.Dtos;

namespace PriceWatcher.Services.Interfaces;

public interface ISearchStatusService
{
    void Initialize(Guid searchId);
    void MarkProcessing(Guid searchId);
    void Complete(Guid searchId, IEnumerable<ProductCandidateDto> results);
    void Fail(Guid searchId, string message);
    SearchStatusDto? GetStatus(Guid searchId);
}

