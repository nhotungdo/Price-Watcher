namespace PriceWatcher.Dtos
{
    public class BuySignalDto
    {
        public string? Signal { get; set; } // "GOOD", "HOLD", "HIGH"
        public double Confidence { get; set; }
        public string? Reason { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? Percentile10 { get; set; }
        public decimal? Median { get; set; }
        public decimal? BestPrice { get; set; }
    }
}
