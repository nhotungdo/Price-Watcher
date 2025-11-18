using System;
using System.Collections.Generic;

namespace PriceWatcher.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string Email { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? GoogleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public virtual ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
}
