using Microsoft.AspNetCore.Mvc;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Services;
using AiAnalysisArtifact = PolityKit.Sim.Analysis.AiAnalysisArtifact;
using AiAnalysisResult = PolityKit.Sim.Analysis.AiAnalysisResult;
using AiAnalysisStatus = PolityKit.Sim.Analysis.AiAnalysisStatus;
using AiBatchAnomalyArtifact = PolityKit.Sim.Analysis.AiBatchAnomalyArtifact;
using AiModelCritiqueArtifact = PolityKit.Sim.Analysis.AiModelCritiqueArtifact;
using AiScenarioSuggestionArtifact = PolityKit.Sim.Analysis.AiScenarioSuggestionArtifact;

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

    [HttpPost("stress/ai/anomalies")]
    public async Task<IActionResult> CreateStressAnomalies(
        [FromBody] StressSweepRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return ToAiArtifactResponse(await simulationRunService.CreateStressAnomaliesAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Stress anomaly request is invalid.",
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

    [HttpPost("{id:guid}/ai-summary")]
    [HttpPost("{id:guid}/ai/summary")]
    public async Task<IActionResult> CreateAiSummary(Guid id, CancellationToken cancellationToken)
    {
        var artifact = await simulationRunService.CreateRunSummaryAsync(id, cancellationToken);
        return ToAiArtifactResponse(artifact);
    }

    [HttpPost("{id:guid}/scenario-suggestions")]
    [HttpPost("{id:guid}/ai/scenario-suggestions")]
    public async Task<IActionResult> CreateScenarioSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var artifact = await simulationRunService.CreateScenarioSuggestionAsync(id, cancellationToken);
        return ToAiArtifactResponse(artifact);
    }

    [HttpPost("{id:guid}/model-critique")]
    [HttpPost("{id:guid}/ai/model-critique")]
    public async Task<IActionResult> CreateModelCritique(
        Guid id,
        [FromQuery] string? model,
        CancellationToken cancellationToken)
    {
        try
        {
            var models = string.IsNullOrWhiteSpace(model)
                ? null
                : model.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var artifact = await simulationRunService.CreateModelCritiqueAsync(id, models, cancellationToken);
            return ToAiArtifactResponse(artifact);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Model critique request is invalid.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
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

    private IActionResult ToAiArtifactResponse(AiAnalysisArtifact? artifact)
    {
        if (artifact is null)
        {
            return NotFound();
        }

        return artifact.Result.Status switch
        {
            AiAnalysisStatus.Disabled => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                CreateProblemDetails(
                    "AI analysis is disabled.",
                    artifact.Result.GeneratedText,
                    StatusCodes.Status503ServiceUnavailable)),
            AiAnalysisStatus.Failed => BadRequest(CreateProblemDetails(
                "AI analysis request is invalid.",
                GetAiFailureDetail(artifact.Result),
                StatusCodes.Status400BadRequest)),
            _ => Ok(artifact)
        };
    }

    private IActionResult ToAiArtifactResponse(AiScenarioSuggestionArtifact? artifact)
    {
        if (artifact is null)
        {
            return NotFound();
        }

        var analysisResponse = TryCreateAiFailureResponse(artifact.Analysis);
        if (analysisResponse is not null)
        {
            return analysisResponse;
        }

        return artifact.CanSave
            ? Ok(artifact)
            : UnprocessableEntity(CreateProblemDetails(
                "AI scenario suggestion output is invalid.",
                string.Join(" ", artifact.Validation.Errors),
                StatusCodes.Status422UnprocessableEntity));
    }

    private IActionResult ToAiArtifactResponse(AiModelCritiqueArtifact? artifact)
    {
        if (artifact is null)
        {
            return NotFound();
        }

        return TryCreateAiFailureResponse(artifact.Analysis) ?? Ok(artifact);
    }

    private IActionResult ToAiArtifactResponse(AiBatchAnomalyArtifact artifact)
    {
        var analysisResponse = TryCreateAiFailureResponse(artifact.Analysis);
        if (analysisResponse is not null)
        {
            return analysisResponse;
        }

        return artifact.CanUse
            ? Ok(artifact)
            : UnprocessableEntity(CreateProblemDetails(
                "AI anomaly output is invalid.",
                string.Join(" ", artifact.Validation.Errors),
                StatusCodes.Status422UnprocessableEntity));
    }

    private IActionResult? TryCreateAiFailureResponse(AiAnalysisArtifact artifact)
    {
        return artifact.Result.Status switch
        {
            AiAnalysisStatus.Disabled => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                CreateProblemDetails(
                    "AI analysis is disabled.",
                    artifact.Result.GeneratedText,
                    StatusCodes.Status503ServiceUnavailable)),
            AiAnalysisStatus.Failed => BadRequest(CreateProblemDetails(
                "AI analysis request is invalid.",
                GetAiFailureDetail(artifact.Result),
                StatusCodes.Status400BadRequest)),
            _ => null
        };
    }

    private static ProblemDetails CreateProblemDetails(string title, string detail, int status)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status
        };
    }

    private static string GetAiFailureDetail(AiAnalysisResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.GeneratedText))
        {
            return result.GeneratedText;
        }

        return result.Warnings.Count == 0
            ? "AI analysis failed."
            : string.Join(" ", result.Warnings);
    }
}
