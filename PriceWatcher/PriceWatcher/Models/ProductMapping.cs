using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceWatcher.Models;

public class ProductMapping
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string SourceUrl { get; set; } = string.Empty;

    public string SourceProductId { get; set; } = string.Empty;

    public string MatchedCandidatesJson { get; set; } = string.Empty;

    public DateTime LastSeen { get; set; }
}
