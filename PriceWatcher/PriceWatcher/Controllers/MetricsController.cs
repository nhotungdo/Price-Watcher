using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("metrics")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metrics;
    private readonly ILogger<MetricsController> _logger;
    public MetricsController(IMetricsService metrics, ILogger<MetricsController> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get() => Ok(_metrics.GetSnapshot());

    [HttpPost("js")]
    public IActionResult RecordJsLog([FromBody] JsLogEntry entry)
    {
        if (entry == null) return BadRequest();
        var level = (entry.Level ?? "info").ToLowerInvariant();
        var message = entry.Message ?? string.Empty;
        var context = $"page={entry.Page} ua={entry.UserAgent} src={entry.Source}:{entry.Line}:{entry.Column}";
        switch (level)
        {
            case "error":
                _logger.LogError("JS {Context} :: {Message} :: {Stack}", context, message, entry.Stack);
                break;
            case "warn":
            case "warning":
                _logger.LogWarning("JS {Context} :: {Message}", context, message);
                break;
            default:
                _logger.LogInformation("JS {Context} :: {Message}", context, message);
                break;
        }
        return NoContent();
    }

    public class JsLogEntry
    {
        public string? Level { get; set; }
        public string? Message { get; set; }
        public string? Stack { get; set; }
        public string? Page { get; set; }
        public string? Source { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }
        public string? UserAgent { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
