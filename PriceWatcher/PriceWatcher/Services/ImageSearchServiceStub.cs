using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;
using System.Net.Http.Headers;

namespace PriceWatcher.Services;

public class ImageSearchServiceStub : IImageSearchService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IImageEmbeddingService _embedding;
    private readonly ILogger<ImageSearchServiceStub> _logger;

    public ImageSearchServiceStub(IHttpClientFactory httpClientFactory, IImageEmbeddingService embedding, ILogger<ImageSearchServiceStub> logger)
    {
        _httpClientFactory = httpClientFactory;
        _embedding = embedding;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductQuery>> SearchByImageAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        byte[] processed;
        try
        {
            processed = await _embedding.PreprocessAsync(imageStream, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Lỗi xử lý ảnh: {ex.Message}");
        }

        var list = new List<ProductQuery>();

        await TryShopeeAsync(processed, list, cancellationToken);
        await TryLazadaAsync(processed, list, cancellationToken);
        await TryTikiAsync(processed, list, cancellationToken);

        if (list.Count == 0)
        {
            list.Add(new ProductQuery { Platform = "shopee", ProductId = "", CanonicalUrl = "https://shopee.vn/", TitleHint = "Sản phẩm từ ảnh" });
            list.Add(new ProductQuery { Platform = "lazada", ProductId = "", CanonicalUrl = "https://www.lazada.vn/", TitleHint = "Sản phẩm từ ảnh" });
            list.Add(new ProductQuery { Platform = "tiki", ProductId = "", CanonicalUrl = "https://tiki.vn/", TitleHint = "Sản phẩm từ ảnh" });
        }
        return list;
    }

    private async Task TryShopeeAsync(byte[] imageBytes, List<ProductQuery> list, CancellationToken ct)
    {
        try
        {
            var http = _httpClientFactory.CreateClient("shopee");
            var url = "https://shopee.vn/api/v4/search/search_image";
            using var content = new MultipartFormDataContent();
            var imgPart = new ByteArrayContent(imageBytes);
            imgPart.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imgPart, "image", "upload.jpg");
            var res = await http.PostAsync(url, content, ct);
            if (!res.IsSuccessStatusCode) return;
            using var doc = System.Text.Json.JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("keyword", out var kw))
            {
                var k = kw.GetString() ?? string.Empty;
                list.Add(new ProductQuery { Platform = "shopee", ProductId = string.Empty, CanonicalUrl = "https://shopee.vn/", TitleHint = k });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shopee image search failed");
        }
    }

    private async Task TryLazadaAsync(byte[] imageBytes, List<ProductQuery> list, CancellationToken ct)
    {
        try
        {
            var http = _httpClientFactory.CreateClient("lazada");
            var url = "https://www.lazada.vn/visual/search";
            using var content = new MultipartFormDataContent();
            var imgPart = new ByteArrayContent(imageBytes);
            imgPart.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imgPart, "file", "upload.jpg");
            var res = await http.PostAsync(url, content, ct);
            if (!res.IsSuccessStatusCode) return;
            using var doc = System.Text.Json.JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("query", out var kw))
            {
                var k = kw.GetString() ?? string.Empty;
                list.Add(new ProductQuery { Platform = "lazada", ProductId = string.Empty, CanonicalUrl = "https://www.lazada.vn/", TitleHint = k });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lazada visual search failed");
        }
    }

    private async Task TryTikiAsync(byte[] imageBytes, List<ProductQuery> list, CancellationToken ct)
    {
        try
        {
            var http = _httpClientFactory.CreateClient("tiki");
            var url = "https://tiki.vn/api/v2/image-search";
            using var content = new MultipartFormDataContent();
            var imgPart = new ByteArrayContent(imageBytes);
            imgPart.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imgPart, "image", "upload.jpg");
            var res = await http.PostAsync(url, content, ct);
            if (!res.IsSuccessStatusCode) return;
            using var doc = System.Text.Json.JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("keyword", out var kw))
            {
                var k = kw.GetString() ?? string.Empty;
                list.Add(new ProductQuery { Platform = "tiki", ProductId = string.Empty, CanonicalUrl = "https://tiki.vn/", TitleHint = k });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tiki image search failed");
        }
    }
}

