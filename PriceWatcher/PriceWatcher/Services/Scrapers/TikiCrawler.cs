using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using PriceWatcher.Models;

namespace PriceWatcher.Services.Scrapers
{
    public interface ITikiCrawler
    {
        Task<CrawledProduct> CrawlProductAsync(string url);
    }

    public class CrawledProduct
    {
        public string SourceProductId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int? DiscountPercent { get; set; }
        public string MainImageUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public string ShopName { get; set; }
        public double? Rating { get; set; }
        public int? ReviewCount { get; set; }
        public int? SoldQuantity { get; set; }
        public string Category { get; set; }
        public string StockStatus { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TikiCrawler : ITikiCrawler
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TikiCrawler> _logger;

        public TikiCrawler(IHttpClientFactory httpClientFactory, ILogger<TikiCrawler> logger)
        {
            _httpClient = httpClientFactory.CreateClient("tiki");
            _logger = logger;
        }

        public async Task<CrawledProduct> CrawlProductAsync(string url)
        {
            var result = new CrawledProduct { IsSuccess = false };

            try
            {
                // Basic validation
                if (!url.Contains("tiki.vn"))
                {
                    result.ErrorMessage = "Invalid Tiki URL";
                    return result;
                }

                // Extract Product ID from URL (simple regex or split)
                // Example: https://tiki.vn/iphone-15-pro-max-p271966088.html -> 271966088
                var productId = ExtractProductId(url);
                if (string.IsNullOrEmpty(productId))
                {
                    result.ErrorMessage = "Could not extract Product ID from URL";
                    return result;
                }
                result.SourceProductId = productId;

                // Fetch HTML
                // Note: Tiki uses client-side rendering heavily. 
                // However, for SEO, they often include JSON-LD or initial state in the HTML.
                // We will try to parse the HTML first. If that fails, we might need to call their API directly.
                // API Endpoint: https://tiki.vn/api/v2/products/{productId}
                
                // Let's try the API approach as it's more reliable for Tiki than parsing dynamic HTML
                var apiUrl = $"https://tiki.vn/api/v2/products/{productId}";
                var response = await _httpClient.GetAsync(apiUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"Failed to fetch data from Tiki API. Status: {response.StatusCode}";
                    return result;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                
                // Parse JSON
                // We can use System.Text.Json or Newtonsoft.Json
                using (var doc = System.Text.Json.JsonDocument.Parse(jsonContent))
                {
                    var root = doc.RootElement;

                    result.Title = root.GetProperty("name").GetString();
                    result.Price = root.GetProperty("price").GetDecimal();
                    
                    if (root.TryGetProperty("original_price", out var originalPriceProp))
                    {
                        result.OriginalPrice = originalPriceProp.GetDecimal();
                    }

                    if (root.TryGetProperty("discount_rate", out var discountRateProp))
                    {
                        result.DiscountPercent = discountRateProp.GetInt32();
                    }

                    if (root.TryGetProperty("thumbnail_url", out var thumbProp))
                    {
                        result.MainImageUrl = thumbProp.GetString();
                    }
                    
                    if (root.TryGetProperty("current_seller", out var sellerProp) && sellerProp.ValueKind != System.Text.Json.JsonValueKind.Null)
                    {
                        if (sellerProp.TryGetProperty("name", out var sellerNameProp))
                        {
                            result.ShopName = sellerNameProp.GetString();
                        }
                    }

                    if (root.TryGetProperty("rating_average", out var ratingProp))
                    {
                        result.Rating = ratingProp.GetDouble();
                    }

                    if (root.TryGetProperty("review_count", out var reviewCountProp))
                    {
                        result.ReviewCount = reviewCountProp.GetInt32();
                    }
                    
                    if (root.TryGetProperty("all_time_quantity_sold", out var soldProp))
                    {
                         result.SoldQuantity = soldProp.GetInt32();
                    }
                    else if (root.TryGetProperty("quantity_sold", out var quantitySoldProp) && quantitySoldProp.TryGetProperty("value", out var soldValueProp))
                    {
                        result.SoldQuantity = soldValueProp.GetInt32();
                    }

                    // Stock status
                    if (root.TryGetProperty("inventory_status", out var inventoryProp))
                    {
                        result.StockStatus = inventoryProp.GetString() == "available" ? "InStock" : "OutOfStock";
                    }
                    else
                    {
                        result.StockStatus = "InStock"; // Default
                    }
                }

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling Tiki product: {Url}", url);
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private string ExtractProductId(string url)
        {
            try
            {
                // Regex to find p{digits}
                var match = System.Text.RegularExpressions.Regex.Match(url, @"-p(\d+)\.html");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                
                // Fallback: check query param ?spid=...
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                if (query["spid"] != null)
                {
                    // Sometimes spid is the variant ID, but we might need the main ID.
                    // Let's assume the regex is the primary way.
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
