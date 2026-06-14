using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Services.Models;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Engine;
using PolityKit.Sim.Metrics;
using PolityKit.Sim.Models;
using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Api.Services;

public sealed class SimulationRunService(
    ISimulationEngine simulationEngine,
    IModelCatalog modelCatalog,
    IMetricCatalog metricCatalog,
    ScenarioResolver scenarioResolver,
    IRunStore runStore)
{
    public StoredRun CreateRun(CreateRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scenario = scenarioResolver.Resolve(request.Scenario);
        var seed = request.Seed ?? scenario.Seed;
        var ticks = request.Ticks ?? scenario.Ticks;
        var models = SelectModels(request.Models);
        var parameters = request.Parameters ?? new Dictionary<string, double>();

        return RunAndStore(scenario.Name, seed, ticks, models, parameters);
    }

    public StoredRun? Rerun(Guid id, RerunRequest? request)
    {
        var existingRun = runStore.Get(id);
        if (existingRun is null)
        {
            return null;
        }

        var configuration = GetConfiguration(existingRun);
        var modelNames = request?.Models is { Count: > 0 }
            ? request.Models
            : configuration.ModelNames;
        var parameters = new Dictionary<string, double>(configuration.Parameters, StringComparer.OrdinalIgnoreCase);
        if (request?.Parameters is not null)
        {
            foreach (var parameter in request.Parameters)
            {
                parameters[parameter.Key] = parameter.Value;
            }
        }

        var models = SelectModels(modelNames);
        return RunAndStore(
            configuration.ScenarioName,
            configuration.Seed,
            request?.Ticks ?? configuration.Ticks,
            models,
            parameters);
    }

    private StoredRun RunAndStore(
        string scenarioName,
        int seed,
        int ticks,
        IReadOnlyList<ISystemModel> models,
        IReadOnlyDictionary<string, double> parameters)
    {
        var scenario = scenarioResolver.Resolve(scenarioName);
        scenario = scenario.WithSeed(seed).WithTicks(ticks);
        var result = simulationEngine.Run(new SimulationRunRequest
        {
            Scenario = scenario,
            Seed = seed,
            Models = models,
            Metrics = metricCatalog.All,
            Parameters = parameters
        });

        return runStore.Add(new StoredRun
        {
            Configuration = new RunConfiguration
            {
                ScenarioName = scenario.Name,
                Seed = result.Seed,
                Ticks = result.Ticks,
                ModelNames = models.Select(model => model.Name).ToList(),
                Parameters = new Dictionary<string, double>(parameters)
            },
            Result = result
        });
    }

    private IReadOnlyList<ISystemModel> SelectModels(IReadOnlyList<string>? requestedModels)
    {
        if (requestedModels is null || requestedModels.Count == 0)
        {
            return modelCatalog.All;
        }

        return requestedModels
            .Select(modelName => modelCatalog.FindByName(modelName)
                ?? throw new InvalidOperationException($"Unknown model '{modelName}'."))
            .ToArray();
    }

    private static RunConfiguration GetConfiguration(StoredRun run)
    {
        if (!string.IsNullOrWhiteSpace(run.Configuration.ScenarioName))
        {
            return run.Configuration;
        }

        return new RunConfiguration
        {
            ScenarioName = run.Result.ScenarioName,
            Seed = run.Result.Seed,
            Ticks = run.Result.Ticks,
            ModelNames = run.Result.ModelResults.Select(model => model.ModelName).ToList()
        };
    }
}
