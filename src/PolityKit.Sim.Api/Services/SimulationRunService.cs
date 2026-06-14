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
    private const int MaxSweepRuns = 256;

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

    public ParameterSweepResponse CreateSweep(ParameterSweepRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scenario = scenarioResolver.Resolve(request.Scenario);
        var seed = request.Seed ?? scenario.Seed;
        var ticks = request.Ticks ?? scenario.Ticks;
        var models = SelectModels(request.Models);
        var baseParameters = request.Parameters ?? new Dictionary<string, double>();
        var sweep = NormalizeSweep(request.Sweep);
        var combinations = BuildParameterCombinations(baseParameters, sweep);

        var runs = combinations
            .Select(parameters => new
            {
                Parameters = parameters,
                StoredRun = RunAndStore(scenario.Name, seed, ticks, models, parameters)
            })
            .Select(item => new ParameterSweepRunResponse
            {
                Run = RunMappers.ToSummaryResponse(item.StoredRun),
                Parameters = item.Parameters,
                FinalMetrics = RunMappers.ToFinalMetrics(item.StoredRun)
            })
            .ToArray();

        return new ParameterSweepResponse
        {
            ScenarioName = scenario.Name,
            Seed = seed,
            Ticks = ticks,
            RunCount = runs.Length,
            Sweep = sweep.ToDictionary(
                item => item.Key,
                item => (IReadOnlyList<double>)item.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase),
            Runs = runs
        };
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

    private static IReadOnlyDictionary<string, IReadOnlyList<double>> NormalizeSweep(
        IReadOnlyDictionary<string, IReadOnlyList<double>>? sweep)
    {
        if (sweep is null || sweep.Count == 0)
        {
            throw new InvalidOperationException("At least one sweep parameter is required.");
        }

        var normalized = new Dictionary<string, IReadOnlyList<double>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, values) in sweep.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Sweep parameter names cannot be blank.");
            }

            if (values.Count == 0)
            {
                throw new InvalidOperationException($"Sweep parameter '{name}' must include at least one value.");
            }

            normalized[name] = values.ToArray();
        }

        var runCount = normalized.Values.Aggregate(1, (count, values) => count * values.Count);
        if (runCount > MaxSweepRuns)
        {
            throw new InvalidOperationException($"Sweep would create {runCount} runs; the maximum is {MaxSweepRuns}.");
        }

        return normalized;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, double>> BuildParameterCombinations(
        IReadOnlyDictionary<string, double> baseParameters,
        IReadOnlyDictionary<string, IReadOnlyList<double>> sweep)
    {
        var combinations = new List<Dictionary<string, double>>
        {
            new(baseParameters, StringComparer.OrdinalIgnoreCase)
        };

        foreach (var (name, values) in sweep)
        {
            combinations = combinations
                .SelectMany(existing => values.Select(value =>
                {
                    var next = new Dictionary<string, double>(existing, StringComparer.OrdinalIgnoreCase)
                    {
                        [name] = value
                    };
                    return next;
                }))
                .ToList();
        }

        return combinations;
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
