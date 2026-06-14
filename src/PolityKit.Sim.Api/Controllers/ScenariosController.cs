using Microsoft.AspNetCore.Mvc;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Api.Controllers;

[ApiController]
[Route("api/scenarios")]
public sealed class ScenariosController(IScenarioCatalog scenarioCatalog) : ControllerBase
{
    [HttpGet]
    public IActionResult GetScenarios()
    {
        return Ok(scenarioCatalog.All.Select(scenario => new ScenarioResponse
        {
            Name = scenario.Name,
            Slug = ScenarioNames.ToSlug(scenario.Name),
            Seed = scenario.Seed,
            Ticks = scenario.Ticks,
            InitialPopulation = scenario.InitialPopulation,
            Shocks = scenario.Shocks.Select(shock => new ShockResponse
            {
                Tick = shock.Tick,
                Type = shock.Type,
                Severity = shock.Severity
            }).ToArray()
        }));
    }
}
