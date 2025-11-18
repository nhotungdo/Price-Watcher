using System;
using System.Collections.Generic;

namespace PriceWatcher.Models;

public partial class Platform
{
    public int PlatformId { get; set; }

    public string PlatformName { get; set; } = null!;

    public string? Domain { get; set; }

    public string? ColorCode { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
