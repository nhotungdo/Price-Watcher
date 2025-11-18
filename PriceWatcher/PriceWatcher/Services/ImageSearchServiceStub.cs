using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class ImageSearchServiceStub : IImageSearchService
{
    private static readonly ProductQuery[] MockQueries =
    {
        new()
        {
            Platform = "shopee",
            ProductId = "i.12345.67890",
            CanonicalUrl = "https://shopee.vn/product/12345/67890",
            TitleHint = "mocked shopee product"
        },
        new()
        {
            Platform = "lazada",
            ProductId = "998877",
            CanonicalUrl = "https://www.lazada.vn/products/mock-998877.html",
            TitleHint = "mocked lazada product"
        }
    };

    public Task<IEnumerable<ProductQuery>> SearchByImageAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ProductQuery>>(MockQueries);
    }
}

