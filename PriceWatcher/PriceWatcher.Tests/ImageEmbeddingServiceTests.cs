using System.Drawing;
using PriceWatcher.Services;

namespace PriceWatcher.Tests;

public class ImageEmbeddingServiceTests
{
    [Fact]
    public async Task EmbeddingSimilarity_SameImage_IsHigh()
    {
        var svc = new ImageEmbeddingService();
        using var bmp1 = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp1))
        {
            g.Clear(Color.White);
            g.FillRectangle(Brushes.Black, 8, 8, 16, 16);
        }
        using var ms1 = new MemoryStream();
        bmp1.Save(ms1, System.Drawing.Imaging.ImageFormat.Png);
        ms1.Position = 0;

        using var bmp2 = (Bitmap)bmp1.Clone();
        using var ms2 = new MemoryStream();
        bmp2.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
        ms2.Position = 0;

        var e1 = await svc.ComputeEmbeddingAsync(ms1);
        var e2 = await svc.ComputeEmbeddingAsync(ms2);
        var sim = svc.CosineSimilarity(e1, e2);
        Assert.True(sim > 0.95);
    }

    [Fact]
    public async Task EmbeddingSimilarity_DifferentImage_IsLow()
    {
        var svc = new ImageEmbeddingService();
        using var bmp1 = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp1))
        {
            g.Clear(Color.White);
            g.FillRectangle(Brushes.Black, 8, 8, 16, 16);
        }
        using var ms1 = new MemoryStream();
        bmp1.Save(ms1, System.Drawing.Imaging.ImageFormat.Png);
        ms1.Position = 0;

        using var bmp2 = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp2))
        {
            g.Clear(Color.Black);
            g.FillEllipse(Brushes.White, 8, 8, 16, 16);
        }
        using var ms2 = new MemoryStream();
        bmp2.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
        ms2.Position = 0;

        var e1 = await svc.ComputeEmbeddingAsync(ms1);
        var e2 = await svc.ComputeEmbeddingAsync(ms2);
        var sim = svc.CosineSimilarity(e1, e2);
        Assert.True(sim < 0.7);
    }
}