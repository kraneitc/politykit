using Microsoft.AspNetCore.Mvc;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Services;

namespace PolityKit.Sim.Api.Controllers;

[ApiController]
[Route("api/runs")]
public sealed class RunsController(SimulationRunService simulationRunService, IRunStore runStore) : ControllerBase
{
    [HttpGet]
    public IActionResult GetRuns()
    {
        return Ok(runStore.List().Select(RunMappers.ToSummaryResponse));
    }

    [HttpPost]
    public IActionResult CreateRun([FromBody] CreateRunRequest request)
    {
        try
        {
            var storedRun = simulationRunService.CreateRun(request);
            return CreatedAtAction(nameof(GetRun), new { id = storedRun.Id }, RunMappers.ToDetailResponse(storedRun));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Run request is invalid.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("sweep")]
    public IActionResult CreateSweep([FromBody] ParameterSweepRequest request)
    {
        try
        {
            return Ok(simulationRunService.CreateSweep(request));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Sweep request is invalid.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("stress")]
    public IActionResult CreateStress([FromBody] StressSweepRequest request)
    {
        try
        {
            return Ok(simulationRunService.CreateStress(request));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Stress sweep request is invalid.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("{id:guid}/rerun")]
    public IActionResult Rerun(Guid id, [FromBody] RerunRequest? request)
    {
        try
        {
            var storedRun = simulationRunService.Rerun(id, request);
            return storedRun is null
                ? NotFound()
                : CreatedAtAction(nameof(GetRun), new { id = storedRun.Id }, RunMappers.ToDetailResponse(storedRun));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Run request is invalid.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetRun(Guid id)
    {
        var storedRun = runStore.Get(id);
        return storedRun is null
            ? NotFound()
            : Ok(RunMappers.ToDetailResponse(storedRun));
    }

    [HttpGet("{id:guid}/metrics")]
    public IActionResult GetRunMetrics(Guid id)
    {
        var storedRun = runStore.Get(id);
        return storedRun is null
            ? NotFound()
            : Ok(RunMappers.ToMetrics(storedRun));
    }

    [HttpGet("{id:guid}/events")]
    public IActionResult GetRunEvents(Guid id)
    {
        var storedRun = runStore.Get(id);
        return storedRun is null
            ? NotFound()
            : Ok(RunMappers.ToEvents(storedRun));
    }

    [HttpGet("{id:guid}/dashboard")]
    public IActionResult GetRunDashboard(Guid id)
    {
        var storedRun = runStore.Get(id);
        return storedRun is null
            ? NotFound()
            : Ok(RunMappers.ToDashboardResponse(storedRun));
    }

    [HttpGet("{id:guid}/compare/{comparisonId:guid}")]
    public IActionResult CompareRuns(Guid id, Guid comparisonId)
    {
        var baseline = runStore.Get(id);
        var comparison = runStore.Get(comparisonId);
        return baseline is null || comparison is null
            ? NotFound()
            : Ok(RunMappers.ToComparisonResponse(baseline, comparison));
    }
}
