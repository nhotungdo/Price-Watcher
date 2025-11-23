namespace PriceWatcher.Dtos
{
    public class CompareRequestDto
    {
        public string SourceUrl { get; set; } = string.Empty;
    }

    public class ProductDto
    {
        public string Platform { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal PriceRaw { get; set; }
        public string Currency { get; set; } = "VND";
        public List<string> Images { get; set; } = new();
        public Dictionary<string, string> Attributes { get; set; } = new();
        public string SellerId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class CandidateDto
    {
        public ProductDto Product { get; set; } = new();
        public decimal ShippingEstimate { get; set; }
        public decimal TotalPriceNormalized { get; set; }
        public double MatchScore { get; set; }
        public List<string> MatchReasons { get; set; } = new();
    }

    public class BestPriceDto
    {
        public string Platform { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public decimal TotalPriceVnd { get; set; }
        public double Confidence { get; set; }
    }

    public class CompareResultDto
    {
        public ProductDto SourceProduct { get; set; } = new();
        public List<CandidateDto> Candidates { get; set; } = new();
        public BestPriceDto BestPrice { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
