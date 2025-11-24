using System;

namespace PriceWatcher.Dtos
{
    public class ProductComparisonDto
    {
        public int ProductId { get; set; }
        public int PlatformId { get; set; }
        public string? PlatformName { get; set; }
        public string? PlatformLogo { get; set; }
        public string? Domain { get; set; }
        public string? ColorCode { get; set; }
        public string? ShopName { get; set; }
        public decimal? Price { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? AffiliateUrl { get; set; }
        public string? OriginalUrl { get; set; }
        public string? ShippingInfo { get; set; }
        public bool IsCheapest { get; set; }
        public double? Rating { get; set; }
        public int? ReviewCount { get; set; }
        public string? ProductName { get; set; }
        public string? ImageUrl { get; set; }
    }
}
