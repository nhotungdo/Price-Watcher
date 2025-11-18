using System;
using System.Collections.Generic;

namespace PriceWatcher.Models;

public partial class SearchHistory
{
    public int HistoryId { get; set; }

    public int? UserId { get; set; }

    public string? SearchType { get; set; }

    public string? InputContent { get; set; }

    public string? DetectedKeyword { get; set; }

    public decimal? BestPriceFound { get; set; }

    public DateTime? SearchTime { get; set; }

    public virtual User? User { get; set; }
}
