using Microsoft.AspNetCore.Mvc;
using PolityKit.Sim.Metrics;

namespace PolityKit.Sim.Api.Controllers;

[ApiController]
[Route("api/metrics")]
public sealed class MetricsController(IMetricCatalog metricCatalog) : ControllerBase
{
    [HttpGet]
    public IActionResult GetMetrics()
    {
        return Ok(metricCatalog.All.Select(metric => new
        {
            metric.Name
        }));
    }
}
