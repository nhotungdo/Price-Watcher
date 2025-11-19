namespace PriceWatcher.Options;

public class RecommendationOptions
{
    public decimal WeightPrice { get; set; } = 0.7m;
    public decimal WeightRating { get; set; } = 0.2m;
    public decimal WeightShipping { get; set; } = 0.1m;
    public decimal WeightTitleSimilarity { get; set; } = 0.2m;
    public decimal WeightImageSimilarity { get; set; } = 0m;
    public int TrustedShopSalesThreshold { get; set; } = 50;
}

