using System.Numerics;

namespace PriceWatcher.Services.Interfaces;

public interface IImageEmbeddingService
{
    Task<float[]> ComputeEmbeddingAsync(Stream imageStream, CancellationToken cancellationToken = default);
    double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b);
}