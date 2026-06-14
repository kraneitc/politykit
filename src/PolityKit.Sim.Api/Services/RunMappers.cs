using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Services.Models;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Api.Services;

public static class RunMappers
{
    public static RunSummaryResponse ToSummaryResponse(StoredRun run)
    {
        return new RunSummaryResponse
        {
            Id = run.Id,
            CreatedAt = run.CreatedAt,
            ScenarioName = run.Result.ScenarioName,
            Seed = run.Result.Seed,
            Ticks = run.Result.Ticks,
            Models = run.Result.ModelResults.Select(model => model.ModelName).ToArray()
        };
    }

    public static RunDetailResponse ToDetailResponse(StoredRun run)
    {
        return new RunDetailResponse
        {
            Id = run.Id,
            CreatedAt = run.CreatedAt,
            ScenarioName = run.Result.ScenarioName,
            Seed = run.Result.Seed,
            Ticks = run.Result.Ticks,
            Models = run.Result.ModelResults.Select(model => new ModelRunSummaryResponse
            {
                ModelName = model.ModelName,
                ModelVersion = model.ModelVersion,
                EventCount = model.Events.Count,
                FinalMetrics = model.Metrics
                    .GroupBy(metric => metric.Name)
                    .Select(group => group.OrderByDescending(metric => metric.Tick).First())
                    .OrderBy(metric => metric.Name)
                    .Select(metric => new MetricResponse
                    {
                        Model = model.ModelName,
                        Tick = metric.Tick,
                        Name = metric.Name,
                        Value = metric.Value,
                        Unit = metric.Unit
                    })
                    .ToArray()
            }).ToArray()
        };
    }

    public static RunDashboardResponse ToDashboardResponse(StoredRun run)
    {
        return new RunDashboardResponse
        {
            Id = run.Id,
            CreatedAt = run.CreatedAt,
            ScenarioName = run.Result.ScenarioName,
            Seed = run.Result.Seed,
            Ticks = run.Result.Ticks,
            Summary = SimulationRunSummary.Create(run.Result),
            Metrics = ToMetrics(run),
            Events = ToEvents(run)
        };
    }

    public static IReadOnlyList<MetricResponse> ToMetrics(StoredRun run)
    {
        return run.Result.ModelResults
            .SelectMany(model => model.Metrics.Select(metric => new MetricResponse
            {
                Model = model.ModelName,
                Tick = metric.Tick,
                Name = metric.Name,
                Value = metric.Value,
                Unit = metric.Unit
            }))
            .ToArray();
    }

    public static IReadOnlyList<EventResponse> ToEvents(StoredRun run)
    {
        return run.Result.ModelResults
            .SelectMany(model => model.Events.Select(simEvent => new EventResponse
            {
                Model = model.ModelName,
                Tick = simEvent.Tick,
                Type = simEvent.Type,
                Description = simEvent.Description,
                Data = simEvent.Data
            }))
            .ToArray();
    }
}
