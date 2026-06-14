using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Services.Models;
using PolityKit.Sim.Core.Models;
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
        if (request.Seed is not null)
        {
            scenario = scenario.WithSeed(request.Seed.Value);
        }

        if (request.Ticks is not null)
        {
            scenario = scenario.WithTicks(request.Ticks.Value);
        }

        var models = SelectModels(request.Models);
        var result = simulationEngine.Run(new SimulationRunRequest
        {
            Scenario = scenario,
            Seed = request.Seed,
            Models = models,
            Metrics = metricCatalog.All,
            Parameters = request.Parameters ?? new Dictionary<string, double>()
        });

        return runStore.Add(new StoredRun
        {
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
}
