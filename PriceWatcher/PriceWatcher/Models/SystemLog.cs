using System;
using System.Collections.Generic;

namespace PriceWatcher.Models;

public partial class SystemLog
{
    public int LogId { get; set; }

    public string? Level { get; set; }

    public string? Message { get; set; }

    public DateTime? CreatedAt { get; set; }
}
