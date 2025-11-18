using System.Text.RegularExpressions;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Services;

public class LinkProcessor : ILinkProcessor
{
    private static readonly Regex ShopeeRegex = new(@"i\.(?<shop>\d+)\.(?<item>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LazadaRegex = new(@"-s(?<shop>\d+)\.html", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TikiRegex = new(@"-p(?<item>\d+)\.html", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task<ProductQuery> ProcessUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL", nameof(url));
        }

        var host = uri.Host.ToLowerInvariant();

        if (host.Contains("shopee"))
        {
            return Task.FromResult(ProcessShopee(uri));
        }

        if (host.Contains("lazada"))
        {
            return Task.FromResult(ProcessLazada(uri));
        }

        if (host.Contains("tiki"))
        {
            return Task.FromResult(ProcessTiki(uri));
        }

        throw new NotSupportedException("Platform not supported");
    }

    private static ProductQuery ProcessShopee(Uri uri)
    {
        var match = ShopeeRegex.Match(uri.PathAndQuery);
        if (!match.Success)
        {
            throw new InvalidOperationException("Unable to detect Shopee product id.");
        }

        var shopId = match.Groups["shop"].Value;
        var itemId = match.Groups["item"].Value;
        var canonical = $"https://{uri.Host}/product/{shopId}/{itemId}";

        return new ProductQuery
        {
            Platform = "shopee",
            ProductId = $"i.{shopId}.{itemId}",
            CanonicalUrl = canonical,
            TitleHint = ExtractTitleFromPath(uri)
        };
    }

    private static ProductQuery ProcessLazada(Uri uri)
    {
        var match = LazadaRegex.Match(uri.PathAndQuery);
        if (!match.Success)
        {
            throw new InvalidOperationException("Unable to detect Lazada product id.");
        }

        var shopId = match.Groups["shop"].Value;
        var title = ExtractTitleFromPath(uri);
        return new ProductQuery
        {
            Platform = "lazada",
            ProductId = shopId,
            CanonicalUrl = RemoveTracking(uri),
            TitleHint = title
        };
    }

    private static ProductQuery ProcessTiki(Uri uri)
    {
        var match = TikiRegex.Match(uri.PathAndQuery);
        if (!match.Success)
        {
            throw new InvalidOperationException("Unable to detect Tiki product id.");
        }

        var productId = match.Groups["item"].Value;
        return new ProductQuery
        {
            Platform = "tiki",
            ProductId = productId,
            CanonicalUrl = RemoveTracking(uri),
            TitleHint = ExtractTitleFromPath(uri)
        };
    }

    private static string RemoveTracking(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri.ToString();
    }

    private static string? ExtractTitleFromPath(Uri uri)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        var lastSegment = segments[^1];
        var cleaned = lastSegment.Replace(".html", string.Empty, StringComparison.OrdinalIgnoreCase);
        cleaned = cleaned.Replace("-", " ");
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }
}

