using System.Numerics;
using PriceWatcher.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace PriceWatcher.Services;

public class ImageEmbeddingService : IImageEmbeddingService
{
    public async Task<byte[]> PreprocessAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var img = await Image.LoadAsync<Rgba32>(imageStream, cancellationToken);

            ApplyOrientation(img);

            var w = img.Width;
            var h = img.Height;
            var side = Math.Min(Math.Min(w, h), 512);
            var cx = w / 2;
            var cy = h / 2;
            var half = side / 2;
            var rect = new Rectangle(Math.Max(0, cx - half), Math.Max(0, cy - half), side, side);

            using var squared = img.Clone(x => x.Crop(rect).Resize(new Size(side, side)));

            await using var ms = new MemoryStream();
            var encoder = new JpegEncoder { Quality = 85 };
            await squared.SaveAsJpegAsync(ms, encoder, cancellationToken);
            return ms.ToArray();
        }
        catch (SixLabors.ImageSharp.UnknownImageFormatException)
        {
            throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ hoặc file bị hỏng. Vui lòng chọn PNG/JPG/GIF/WEBP.");
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Không thể đọc ảnh. Vui lòng thử lại với file khác.");
        }
    }
    public Task<float[]> ComputeEmbeddingAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var img = Image.Load<Rgba32>(imageStream);
            ApplyOrientation(img);
            using var resized = img.Clone(x => x.Resize(new Size(16, 16)));

            var vec = new float[256];
            var idx = 0;
            resized.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < 16; y++)
                {
                    var span = accessor.GetRowSpan(y);
                    for (var x = 0; x < 16; x++)
                    {
                        var c = span[x];
                        var gray = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
                        vec[idx++] = gray;
                    }
                }
            });
            var mean = vec.Average();
            var std = (float)Math.Sqrt(vec.Select(v => (v - mean) * (v - mean)).Average());
            std = std <= 1e-6f ? 1f : std;
            for (var i = 0; i < vec.Length; i++) vec[i] = (vec[i] - mean) / std;
            return Task.FromResult(vec);
        }
        catch (SixLabors.ImageSharp.UnknownImageFormatException)
        {
            throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ hoặc file bị hỏng. Vui lòng chọn PNG/JPG/GIF/WEBP.");
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Không thể đọc ảnh. Vui lòng thử lại với file khác.");
        }
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

    private static void ApplyOrientation(Image<Rgba32> img)
    {
        try { }
        catch { }
    }
}