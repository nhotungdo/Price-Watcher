using System;
using System.Collections.Generic;

namespace PriceWatcher.Models;

public partial class PriceSnapshot
{
    public int SnapshotId { get; set; }

    public int? ProductId { get; set; }

    public decimal Price { get; set; }

    public DateTime? RecordedAt { get; set; }

    public virtual Product? Product { get; set; }
}
