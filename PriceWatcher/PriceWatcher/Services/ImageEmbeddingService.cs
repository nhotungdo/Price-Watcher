using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class ImageEmbeddingService : IImageEmbeddingService
{
    public Task<float[]> ComputeEmbeddingAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        using var img = Image.FromStream(imageStream);
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.DrawImage(img, 0, 0, 16, 16);
        }

        var vec = new float[256];
        var idx = 0;
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                var c = bmp.GetPixel(x, y);
                var gray = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
                vec[idx++] = gray;
            }
        }
        var mean = vec.Average();
        var std = (float)Math.Sqrt(vec.Select(v => (v - mean) * (v - mean)).Average());
        std = std <= 1e-6f ? 1f : std;
        for (var i = 0; i < vec.Length; i++) vec[i] = (vec[i] - mean) / std;
        return Task.FromResult(vec);
    }

    public double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        if (a.Count != b.Count || a.Count == 0) return 0;
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        if (denom <= 1e-9) return 0;
        return dot / denom;
    }
}