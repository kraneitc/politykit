using PolityKit.Sim.Analysis;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Services.Models;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Engine;
using PolityKit.Sim.Metrics;
using PolityKit.Sim.Models;
using PolityKit.Sim.Scenarios;
using AnalysisStressSweepRequest = PolityKit.Sim.Analysis.StressSweepRequest;
using ApiStressSweepRequest = PolityKit.Sim.Api.Contracts.StressSweepRequest;

namespace PolityKit.Sim.Api.Services;

public sealed class SimulationRunService(
    ISimulationEngine simulationEngine,
    IModelCatalog modelCatalog,
    IMetricCatalog metricCatalog,
    ScenarioResolver scenarioResolver,
    IRunStore runStore,
    AiAnalysisService aiAnalysisService)
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

    public ParameterSweepResponse CreateSweep(ParameterSweepRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scenario = scenarioResolver.Resolve(request.Scenario);
        var seed = request.Seed ?? scenario.Seed;
        var ticks = request.Ticks ?? scenario.Ticks;
        var models = SelectModels(request.Models);
        var baseParameters = request.Parameters ?? new Dictionary<string, double>();
        var sweep = SweepAnalysis.NormalizeSweep(request.Sweep);
        var combinations = SweepAnalysis.BuildParameterCombinations(baseParameters, sweep);

        var runs = combinations
            .Select((parameters, index) =>
            {
                var storedRun = RunAndStore(scenario.Name, seed, ticks, models, parameters);
                var finalMetrics = SweepAnalysis.SelectFinalMetrics(storedRun.Result);

                return new
                {
                    Analysis = new SweepRunReport(index + 1, null, parameters, finalMetrics),
                    Response = new ParameterSweepRunResponse
                    {
                        Run = RunMappers.ToSummaryResponse(storedRun),
                        Parameters = parameters,
                        FinalMetrics = RunMappers.ToMetricResponses(finalMetrics)
                    }
                };
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
            Runs = runs.Select(run => run.Response).ToArray(),
            BestWorst = RunMappers.ToBestWorstResponses(
                SweepAnalysis.BuildBestWorst(runs.Select(run => run.Analysis).ToArray())),
            Sensitivity = SensitivityAnalysis.BuildReport(
                scenario.Name,
                runs.Select(run => run.Analysis).ToArray(),
                baseParameters)
        };
    }

    public StressSweepResponse CreateStress(ApiStressSweepRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestedScenarios = request.Scenarios is { Count: > 0 }
            ? request.Scenarios
            : [scenarioResolver.Resolve(null).Name];
        var requestedSeeds = request.Seeds is { Count: > 0 }
            ? request.Seeds
            : requestedScenarios
                .Select(scenarioName => scenarioResolver.Resolve(scenarioName).Seed)
                .Distinct()
                .ToArray();
        var requestedModels = request.Models is { Count: > 0 }
            ? request.Models
            : modelCatalog.All.Select(model => model.Name).ToArray();
        var plan = StressSweepAnalysis.BuildPlan(new AnalysisStressSweepRequest
        {
            GridName = request.GridName,
            Scenarios = requestedScenarios,
            Seeds = requestedSeeds,
            Models = requestedModels,
            Parameters = request.Parameters,
            Sweep = request.Sweep,
            FailureCriteria = request.FailureCriteria,
            MaxRuns = request.MaxRuns
        });

        var runs = plan.Runs.Select(runPlan =>
        {
            var scenario = scenarioResolver.Resolve(runPlan.Scenario);
            var ticks = request.Ticks ?? scenario.Ticks;
            scenario = scenario.WithSeed(runPlan.Seed).WithTicks(ticks);
            var model = SelectModels([runPlan.Model]);
            var storedRun = RunAndStore(scenario, runPlan.Seed, ticks, model, runPlan.Parameters);
            var finalMetrics = SweepAnalysis.SelectFinalMetrics(storedRun.Result);
            var collapseEvents = FailureAnalysis.DetectCollapses(storedRun.Result, request.FailureCriteria);

            return new
            {
                Analysis = new StressSweepRunResult(
                    runPlan.RunIndex,
                    null,
                    storedRun.Id,
                    storedRun.Result.ScenarioName,
                    storedRun.Result.Seed,
                    storedRun.Result.Ticks,
                    storedRun.Result.ModelResults.Single().ModelName,
                    runPlan.Parameters,
                    finalMetrics,
                    collapseEvents),
                Response = new StressSweepRunResponse
                {
                    RunIndex = runPlan.RunIndex,
                    Run = RunMappers.ToSummaryResponse(storedRun),
                    ScenarioName = storedRun.Result.ScenarioName,
                    Seed = storedRun.Result.Seed,
                    Ticks = storedRun.Result.Ticks,
                    Model = storedRun.Result.ModelResults.Single().ModelName,
                    Parameters = runPlan.Parameters,
                    FinalMetrics = RunMappers.ToMetricResponses(finalMetrics),
                    CollapseEvents = collapseEvents
                }
            };
        }).ToArray();

        var stressRuns = runs.Select(run => run.Analysis).ToArray();
        var sensitivity = SensitivityAnalysis.BuildReport(stressRuns, plan.BaseParameters);

        return new StressSweepResponse
        {
            GridName = plan.GridName,
            Scenarios = stressRuns.Select(run => run.ScenarioName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Seeds = stressRuns.Select(run => run.Seed).Distinct().Order().ToArray(),
            Ticks = request.Ticks,
            Models = stressRuns.Select(run => run.Model).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            BaseParameters = plan.BaseParameters,
            Sweep = plan.Sweep,
            RunCount = runs.Length,
            Runs = runs.Select(run => run.Response).ToArray(),
            BestWorst = RunMappers.ToBestWorstResponses(SweepAnalysis.BuildBestWorst(runs
                .Select(run => new SweepRunReport(
                    run.Analysis.RunIndex,
                    null,
                    run.Analysis.Parameters,
                    run.Analysis.FinalMetrics))
                .ToArray())),
            CollapseEvents = stressRuns.SelectMany(run => run.CollapseEvents).ToArray(),
            Sensitivity = sensitivity,
            ModelRobustness = RobustnessAnalysis.BuildModelSummaries(stressRuns, sensitivity)
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

    public async Task<AiAnalysisArtifact?> CreateRunSummaryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var storedRun = runStore.Get(id);
        if (storedRun is null)
        {
            return null;
        }

        var configuration = GetConfiguration(storedRun);
        var assumptions = SelectAssumptions(configuration.ModelNames);
        var request = AiAnalysisContextBuilders.BuildRunSummaryRequest(
            storedRun.Result,
            configuration.Parameters,
            assumptions,
            storedRun.Id);

        return await aiAnalysisService.AnalyzeAsync(request, cancellationToken).ConfigureAwait(false);
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
        return RunAndStore(scenario, seed, ticks, models, parameters);
    }

    private StoredRun RunAndStore(
        ScenarioDefinition scenario,
        int seed,
        int ticks,
        IReadOnlyList<ISystemModel> models,
        IReadOnlyDictionary<string, double> parameters)
    {
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

    private IReadOnlyList<string> SelectAssumptions(IReadOnlyList<string> modelNames)
    {
        return modelNames
            .Select(modelName => modelCatalog.FindByName(modelName))
            .OfType<AllocationModelBase>()
            .SelectMany(model => model.Manifest.Assumptions
                .Select(assumption => $"{model.Name}: {assumption.Name} - {assumption.Description}"))
            .Where(assumption => !string.IsNullOrWhiteSpace(assumption))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
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
