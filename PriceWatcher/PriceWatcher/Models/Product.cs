using System;
using System.Collections.Generic;

namespace PriceWatcher.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int? PlatformId { get; set; }

    public string? ExternalId { get; set; }

    public string ProductName { get; set; } = null!;

    public string OriginalUrl { get; set; } = null!;

    public string? AffiliateUrl { get; set; }

    public string? ImageUrl { get; set; }

    public decimal? CurrentPrice { get; set; }

    public double? Rating { get; set; }

    public int? ReviewCount { get; set; }

    public string? ShopName { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Platform? Platform { get; set; }

    public virtual ICollection<PriceSnapshot> PriceSnapshots { get; set; } = new List<PriceSnapshot>();
}
