using PriceWatcher.Services;

namespace PriceWatcher.Tests;

public class LinkProcessorTests
{
    private readonly LinkProcessor _processor = new();

    [Theory]
    [InlineData("https://shopee.vn/anything-i.123.456", "shopee", "i.123.456")]
    [InlineData("https://www.lazada.vn/products/some-title-s98765.html?spm=111", "lazada", "98765")]
    [InlineData("https://tiki.vn/san-pham-gi-do-p123456.html", "tiki", "123456")]
    public async Task ProcessUrlAsync_ExtractsPlatformAndId(string url, string expectedPlatform, string expectedId)
    {
        var result = await _processor.ProcessUrlAsync(url);

        Assert.Equal(expectedPlatform, result.Platform);
        Assert.Equal(expectedId, result.ProductId);
        Assert.False(string.IsNullOrWhiteSpace(result.CanonicalUrl));
    }

    [Fact]
    public async Task ProcessUrlAsync_InvalidUrl_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _processor.ProcessUrlAsync("not-a-url"));
    }

    [Fact]
    public async Task ProcessUrlAsync_UnsupportedPlatform_Throws()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _processor.ProcessUrlAsync("https://example.com/product/abc"));
    }
}

