using System;
using System.Collections.Generic;

namespace PriceWatcher.Dtos
{
    public class PriceHistoryDto
    {
        public DateTime Date { get; set; }
        public decimal MinPrice { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public int SampleCount { get; set; }
    }

    public class PriceHistoryResponseDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public List<PriceHistoryDto> History { get; set; }
        public decimal? CurrentPrice { get; set; }
        public decimal? BestPrice { get; set; }
        public DateTime? BestPriceDate { get; set; }
    }
}
