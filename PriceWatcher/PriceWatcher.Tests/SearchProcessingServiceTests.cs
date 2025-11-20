using System.Net.Http;
using Moq;
using PriceWatcher.Dtos;
using PriceWatcher.Services;
using PriceWatcher.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;

public class SearchProcessingServiceTests
{
    private static byte[] MakeSolidImageBytes(byte r, byte g, byte b, int w = 64, int h = 64)
    {
        using var img = new Image<Rgba32>(w, h);
        img.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < h; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < w; x++) row[x] = new Rgba32(r, g, b);
            }
        });
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    private class BytesHttpHandler : HttpMessageHandler
    {
        private readonly byte[] _bytes;
        public BytesHttpHandler(byte[] bytes) { _bytes = bytes; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var resp = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_bytes)
            };
            resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return Task.FromResult(resp);
        }
    }

    [Fact]
    public async Task ProcessAsync_FiltersMismatchedThumbnail_ByEmbeddingSimilarity()
    {
        var link = new Mock<ILinkProcessor>();
        link.Setup(l => l.ProcessUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductQuery { TitleHint = "dove rose" });

        var imageSearch = new Mock<IImageSearchService>();
        var reco = new Mock<IRecommendationService>();
        reco.Setup(r => r.RecommendAsync(It.IsAny<ProductQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ProductCandidateDto { Platform = "tiki", Title = "huggies diaper", Price = 100000, ThumbnailUrl = "http://example.com/thumb.png" } });

        var history = new Mock<ISearchHistoryService>();
        history.Setup(h => h.SaveSearchHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ProductQuery>(), It.IsAny<IEnumerable<ProductCandidateDto>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var status = new SearchStatusService();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<SearchProcessingService>>();
        var embedding = new ImageEmbeddingService();

        var blackThumb = MakeSolidImageBytes(0, 0, 0);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new BytesHttpHandler(blackThumb)));

        var svc = new SearchProcessingService(link.Object, imageSearch.Object, reco.Object, history.Object, status, logger.Object, embedding, factory.Object);

        var whiteSource = MakeSolidImageBytes(255, 255, 255);
        var job = new SearchJob { SearchId = Guid.NewGuid(), UserId = 1, SearchType = "image", ImageBytes = whiteSource, Url = null, QueryOverride = new ProductQuery { TitleHint = "dove rose" } };

        await svc.ProcessAsync(job, CancellationToken.None);
        var st = status.GetStatus(job.SearchId);
        Assert.NotNull(st);
        Assert.NotNull(st!.Status);
        Assert.NotEqual("Failed", st.Status);
    }
}