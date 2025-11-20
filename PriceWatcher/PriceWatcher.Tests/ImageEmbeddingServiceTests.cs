using PriceWatcher.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;
using Moq;
using PriceWatcher.Services.Interfaces;
using Microsoft.Extensions.Logging;
using PriceWatcher.Dtos;

namespace PriceWatcher.Tests;

public class ImageEmbeddingServiceTests
{
    [Fact]
    public async Task EmbeddingSimilarity_SameImage_IsHigh()
    {
        var svc = new ImageEmbeddingService();
        using var img1 = new Image<Rgba32>(32, 32);
        img1.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < img1.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < img1.Width; x++) row[x] = Color.White;
            }
            for (var y = 8; y < 24; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 8; x < 24; x++) row[x] = Color.Black;
            }
        });
        using var ms1 = new MemoryStream();
        await img1.SaveAsPngAsync(ms1);
        ms1.Position = 0;

        using var ms2 = new MemoryStream(ms1.ToArray());

        var e1 = await svc.ComputeEmbeddingAsync(ms1);
        var e2 = await svc.ComputeEmbeddingAsync(ms2);
        var sim = svc.CosineSimilarity(e1, e2);
        Assert.True(sim > 0.95);
    }

    [Fact]
    public async Task EmbeddingSimilarity_DifferentImage_IsLow()
    {
        var svc = new ImageEmbeddingService();
        using var img1 = new Image<Rgba32>(32, 32);
        img1.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < img1.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < img1.Width; x++) row[x] = Color.White;
            }
            for (var y = 8; y < 24; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 8; x < 24; x++) row[x] = Color.Black;
            }
        });
        using var ms1 = new MemoryStream();
        await img1.SaveAsPngAsync(ms1);
        ms1.Position = 0;

        using var img2 = new Image<Rgba32>(32, 32);
        img2.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < img2.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < img2.Width; x++) row[x] = Color.Black;
            }
            for (var y = 12; y < 20; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 12; x < 20; x++) row[x] = Color.White;
            }
        });
        using var ms2 = new MemoryStream();
        await img2.SaveAsPngAsync(ms2);
        ms2.Position = 0;

        var e1 = await svc.ComputeEmbeddingAsync(ms1);
        var e2 = await svc.ComputeEmbeddingAsync(ms2);
        var sim = svc.CosineSimilarity(e1, e2);
        Assert.True(sim < 0.7);
    }

    [Fact]
    public async Task Preprocess_SupportsWebP_ReturnsJpegBytes()
    {
        var svc = new ImageEmbeddingService();
        using var img = new Image<Rgba32>(600, 400);
        img.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < img.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < img.Width; x++) row[x] = Color.White;
            }
            for (var y = 100; y < 300; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 100; x < 300; x++) row[x] = Color.Red;
            }
        });
        using var webp = new MemoryStream();
        await img.SaveAsWebpAsync(webp);
        webp.Position = 0;

        var bytes = await svc.PreprocessAsync(webp);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task Preprocess_InvalidData_Throws()
    {
        var svc = new ImageEmbeddingService();
        using var bad = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.PreprocessAsync(bad));
    }

    [Fact]
    public async Task SearchProcessing_SurfacesImageErrorMessage()
    {
        var link = new Mock<ILinkProcessor>();
        var embed = new Mock<IImageEmbeddingService>();
        var imageSvc = new Mock<IImageSearchService>();
        imageSvc.Setup(s => s.SearchByImageAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Định dạng ảnh không được hỗ trợ hoặc file bị hỏng. Vui lòng chọn PNG/JPG/GIF/WEBP."));
        var reco = new Mock<IRecommendationService>();
        var history = new Mock<ISearchHistoryService>();
        history.Setup(h => h.SaveSearchHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PriceWatcher.Dtos.ProductQuery>(), It.IsAny<IEnumerable<ProductCandidateDto>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var status = new SearchStatusService();
        var logger = new Mock<ILogger<SearchProcessingService>>();
        var embedding = new ImageEmbeddingService();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(new HttpClientHandler()));

        var svc = new SearchProcessingService(link.Object, imageSvc.Object, reco.Object, history.Object, status, logger.Object, embedding, factory.Object);
        var job = new SearchJob { SearchId = Guid.NewGuid(), UserId = 1, SearchType = "image", ImageBytes = new byte[] { 1, 2, 3 } };

        await svc.ProcessAsync(job, CancellationToken.None);
        var st = status.GetStatus(job.SearchId);
        Assert.NotNull(st);
        Assert.Equal("Failed", st!.Status);
        Assert.False(string.IsNullOrWhiteSpace(st.Message));
    }

    [Fact]
    public async Task SearchProcessing_UsesQueryOverrideFromFilename()
    {
        var link = new Mock<ILinkProcessor>();
        var imageSvc = new Mock<IImageSearchService>();
        var capturedQuery = (PriceWatcher.Dtos.ProductQuery?)null;
        var reco = new Mock<IRecommendationService>();
        reco.Setup(r => r.RecommendAsync(It.IsAny<PriceWatcher.Dtos.ProductQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<PriceWatcher.Dtos.ProductQuery, int, CancellationToken>((q, _, __) => capturedQuery = q)
            .ReturnsAsync(Array.Empty<ProductCandidateDto>());
        var history = new Mock<ISearchHistoryService>();
        history.Setup(h => h.SaveSearchHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PriceWatcher.Dtos.ProductQuery>(), It.IsAny<IEnumerable<ProductCandidateDto>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var status = new SearchStatusService();
        var logger = new Mock<ILogger<SearchProcessingService>>();
        var embedding = new ImageEmbeddingService();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(new HttpClientHandler()));

        var svc = new SearchProcessingService(link.Object, imageSvc.Object, reco.Object, history.Object, status, logger.Object, embedding, factory.Object);
        var job = new SearchJob { SearchId = Guid.NewGuid(), UserId = 1, SearchType = "image", ImageBytes = new byte[] { 1, 2, 3 }, QueryOverride = new PriceWatcher.Dtos.ProductQuery { TitleHint = "lux sua tam" } };

        await svc.ProcessAsync(job, CancellationToken.None);
        Assert.NotNull(capturedQuery);
        Assert.Equal("lux sua tam", capturedQuery!.TitleHint);
    }
}