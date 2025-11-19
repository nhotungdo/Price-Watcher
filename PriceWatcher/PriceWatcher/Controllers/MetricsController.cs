using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("metrics")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metrics;
    public MetricsController(IMetricsService metrics)
    {
        _metrics = metrics;
    }

    [HttpGet]
    public IActionResult Get() => Ok(_metrics.GetSnapshot());
}