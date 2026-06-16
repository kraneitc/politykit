using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using PolityKit.Sim.Analysis;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Engine;
using PolityKit.Sim.Metrics;
using PolityKit.Sim.Models;
using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Cli;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                WriteHelp();
                return 0;
            }

            return args[0] switch
            {
                "run" => Run(args[1..]),
                "sweep" => Sweep(args[1..]),
                "stress" => Stress(args[1..]),
                "summary" => Summary(args[1..]),
                "list-models" => ListModels(),
                _ => Fail($"Unknown command '{args[0]}'.")
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            WriteHelp();
            return 0;
        }

        if (options.Sweeps.Count > 0)
        {
            throw new InvalidOperationException("Use the 'sweep' command when passing --sweep options.");
        }

        var scenario = new ScenarioResolver().Resolve(options.Scenario);
        if (options.Seed is not null)
        {
            scenario = scenario.WithSeed(options.Seed.Value);
        }

        if (options.Ticks is not null)
        {
            scenario = scenario.WithTicks(options.Ticks.Value);
        }

        var modelCatalog = new ModelCatalog();
        var models = SelectModels(modelCatalog, options.Models);
        var metrics = DefaultMetricSet.Create();
        var result = new SimulationEngine().Run(new SimulationRunRequest
        {
            Scenario = scenario,
            Seed = options.Seed,
            Models = models,
            Metrics = metrics,
            Parameters = options.Parameters
        });

        var outputDirectory = ResolveOutputDirectory(options.OutputDirectory, scenario.Name, result.Seed);
        WriteRunBundle(outputDirectory, result, scenario, models, metrics, options.Parameters);
        WriteAiAnalysisUsage(outputDirectory);

        Console.WriteLine($"Wrote run output to {outputDirectory}");
        Console.WriteLine($"Scenario: {result.ScenarioName}");
        Console.WriteLine($"Seed: {result.Seed}");
        Console.WriteLine($"Ticks: {result.Ticks}");
        Console.WriteLine($"Models: {string.Join(", ", result.ModelResults.Select(model => model.ModelName))}");

        return 0;
    }

    private static int Summary(string[] args)
    {
        var bundleDirectory = ReadSummaryBundleDirectory(args);
        var providerName = ReadOption(args, "--provider") ?? DisabledAiAnalysisProvider.Name;
        var outputPath = ReadOption(args, "--out") ?? Path.Combine(bundleDirectory, "ai-summary.json");
        var summaryPath = Path.Combine(bundleDirectory, "summary.json");
        var configPath = Path.Combine(bundleDirectory, "config.json");

        if (!File.Exists(summaryPath))
        {
            throw new InvalidOperationException($"Run bundle summary file was not found at '{summaryPath}'.");
        }

        var summary = JsonSerializer.Deserialize<SimulationRunSummary>(File.ReadAllText(summaryPath), JsonOptions)
            ?? throw new InvalidOperationException($"Run bundle summary file '{summaryPath}' could not be read.");
        var config = File.Exists(configPath)
            ? JsonSerializer.Deserialize<RunBundleConfig>(File.ReadAllText(configPath), JsonOptions) ?? new RunBundleConfig()
            : new RunBundleConfig();
        var modelNames = config.Models.Count > 0
            ? config.Models.Select(model => model.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray()
            : summary.Models.Select(model => model.ModelName).ToArray();
        var assumptions = SelectAssumptions(new ModelCatalog(), modelNames);
        var request = AiAnalysisContextBuilders.BuildRunSummaryRequest(
            summary,
            config.Parameters,
            assumptions,
            sourceFiles: File.Exists(configPath) ? [summaryPath, configPath] : [summaryPath]);
        IAiAnalysisProvider provider = string.Equals(providerName, FakeAiAnalysisProvider.Name, StringComparison.OrdinalIgnoreCase)
            ? new FakeAiAnalysisProvider()
            : new DisabledAiAnalysisProvider();
        var artifact = new AiAnalysisService(provider, new AiAnalysisOptions
        {
            Enabled = provider is FakeAiAnalysisProvider,
            ProviderName = provider.ProviderName
        }).AnalyzeAsync(request).GetAwaiter().GetResult();

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        WriteJson(outputPath, artifact);
        Console.WriteLine($"Wrote AI summary artifact to {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Status: {artifact.Result.Status}");
        return 0;
    }

    private static int Stress(string[] args)
    {
        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            WriteHelp();
            return 0;
        }

        var scenarioResolver = new ScenarioResolver();
        var modelCatalog = new ModelCatalog();
        var scenarioNames = options.Scenarios.Count > 0
            ? options.Scenarios
            : [scenarioResolver.Resolve(null).Name];
        var seeds = options.Seeds.Count > 0
            ? options.Seeds
            : scenarioNames
                .Select(name => scenarioResolver.Resolve(name).Seed)
                .Distinct()
                .ToArray();
        var modelNames = ParseModelNames(options.Models)
            ?? modelCatalog.All.Select(model => model.Name).ToArray();
        var plan = StressSweepAnalysis.BuildPlan(new StressSweepRequest
        {
            GridName = options.GridName,
            Scenarios = scenarioNames,
            Seeds = seeds,
            Models = modelNames,
            Parameters = options.Parameters,
            Sweep = options.Sweeps,
            FailureCriteria = options.FailureCriteria,
            MaxRuns = options.MaxRuns
        });

        var metrics = DefaultMetricSet.Create();
        var outputDirectory = ResolveStressOutputDirectory(options.OutputDirectory, plan.GridName);
        var stressRuns = new List<StressSweepRunResult>();

        foreach (var runPlan in plan.Runs)
        {
            var scenario = scenarioResolver.Resolve(runPlan.Scenario);
            var ticks = options.Ticks ?? scenario.Ticks;
            scenario = scenario.WithSeed(runPlan.Seed).WithTicks(ticks);
            var models = SelectModels(modelCatalog, runPlan.Model);
            var result = new SimulationEngine().Run(new SimulationRunRequest
            {
                Scenario = scenario,
                Seed = runPlan.Seed,
                Models = models,
                Metrics = metrics,
                Parameters = runPlan.Parameters
            });

            var runDirectoryName = $"run-{runPlan.RunIndex:000}";
            var runDirectory = Path.Combine(outputDirectory, runDirectoryName);
            Directory.CreateDirectory(runDirectory);
            WriteRunBundle(runDirectory, result, scenario, models, metrics, runPlan.Parameters);
            var finalMetrics = SweepAnalysis.SelectFinalMetrics(result);
            var collapseEvents = FailureAnalysis.DetectCollapses(result, options.FailureCriteria);

            stressRuns.Add(new StressSweepRunResult(
                runPlan.RunIndex,
                runDirectoryName,
                null,
                result.ScenarioName,
                result.Seed,
                result.Ticks,
                result.ModelResults.Single().ModelName,
                runPlan.Parameters,
                finalMetrics,
                collapseEvents));
        }

        var bestWorst = SweepAnalysis.BuildBestWorst(stressRuns
            .Select(run => new SweepRunReport(run.RunIndex, run.Directory, run.Parameters, run.FinalMetrics))
            .ToArray());
        var sensitivity = SensitivityAnalysis.BuildReport(stressRuns, plan.BaseParameters);
        var stressResult = new StressSweepResult(
            plan.GridName,
            stressRuns.Select(run => run.ScenarioName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            stressRuns.Select(run => run.Seed).Distinct().Order().ToArray(),
            stressRuns.Select(run => run.Model).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            plan.BaseParameters,
            plan.Sweep,
            stressRuns.Count,
            stressRuns,
            bestWorst,
            stressRuns.SelectMany(run => run.CollapseEvents).ToArray(),
            sensitivity,
            RobustnessAnalysis.BuildModelSummaries(stressRuns, sensitivity));

        WriteStressMetricsCsv(Path.Combine(outputDirectory, "stress-metrics.csv"), stressRuns);
        WriteJson(Path.Combine(outputDirectory, "stress-summary.json"), stressResult);
        WriteAiAnalysisUsage(outputDirectory);

        Console.WriteLine($"Wrote stress output to {outputDirectory}");
        Console.WriteLine($"Scenarios: {string.Join(", ", stressResult.Scenarios)}");
        Console.WriteLine($"Seeds: {string.Join(", ", stressResult.Seeds)}");
        Console.WriteLine($"Models: {string.Join(", ", stressResult.Models)}");
        Console.WriteLine($"Runs: {stressResult.RunCount}");

        return 0;
    }

    private static int Sweep(string[] args)
    {
        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            WriteHelp();
            return 0;
        }

        var scenario = new ScenarioResolver().Resolve(options.Scenario);
        if (options.Seed is not null)
        {
            scenario = scenario.WithSeed(options.Seed.Value);
        }

        if (options.Ticks is not null)
        {
            scenario = scenario.WithTicks(options.Ticks.Value);
        }

        var modelCatalog = new ModelCatalog();
        var models = SelectModels(modelCatalog, options.Models);
        var metrics = DefaultMetricSet.Create();
        var sweep = SweepAnalysis.NormalizeSweep(options.Sweeps);
        var combinations = SweepAnalysis.BuildParameterCombinations(options.Parameters, sweep);
        var outputDirectory = ResolveSweepOutputDirectory(options.OutputDirectory, scenario.Name, scenario.Seed);
        var sweepRuns = new List<SweepRunReport>();

        for (var index = 0; index < combinations.Count; index++)
        {
            var parameters = combinations[index];
            var result = new SimulationEngine().Run(new SimulationRunRequest
            {
                Scenario = scenario,
                Seed = scenario.Seed,
                Models = models,
                Metrics = metrics,
                Parameters = parameters
            });
            var runDirectoryName = $"run-{index + 1:000}";
            var runDirectory = Path.Combine(outputDirectory, runDirectoryName);
            Directory.CreateDirectory(runDirectory);
            WriteRunBundle(runDirectory, result, scenario, models, metrics, parameters);

            sweepRuns.Add(new SweepRunReport(
                index + 1,
                runDirectoryName,
                parameters,
                SweepAnalysis.SelectFinalMetrics(result)));
        }

        var bestWorst = SweepAnalysis.BuildBestWorst(sweepRuns);
        WriteSweepMetricsCsv(Path.Combine(outputDirectory, "sweep-metrics.csv"), sweepRuns);
        WriteJson(Path.Combine(outputDirectory, "sweep-summary.json"), new
        {
            scenario,
            seed = scenario.Seed,
            ticks = scenario.Ticks,
            models = models.Select(model => new
            {
                model.Name,
                model.Version
            }),
            baseParameters = options.Parameters,
            sweep,
            runCount = sweepRuns.Count,
            runs = sweepRuns.Select(run => new
            {
                run.RunIndex,
                run.Directory,
                run.Parameters,
                finalMetrics = run.FinalMetrics
            }),
            bestWorst,
            sensitivity = SensitivityAnalysis.BuildReport(scenario.Name, sweepRuns, options.Parameters),
            aiAnalysis = AiAnalysisUsage.NotUsed()
        });
        WriteAiAnalysisUsage(outputDirectory);

        Console.WriteLine($"Wrote sweep output to {outputDirectory}");
        Console.WriteLine($"Scenario: {scenario.Name}");
        Console.WriteLine($"Seed: {scenario.Seed}");
        Console.WriteLine($"Ticks: {scenario.Ticks}");
        Console.WriteLine($"Runs: {sweepRuns.Count}");

        return 0;
    }

    private static int ListModels()
    {
        var catalog = new ModelCatalog();
        foreach (var model in catalog.All)
        {
            if (model is CompositeGovernanceModel compositeModel)
            {
                Console.WriteLine($"{model.Name} ({model.Version}) preset:{compositeModel.Profile.Id}");
                continue;
            }

            Console.WriteLine($"{model.Name} ({model.Version})");
        }

        return 0;
    }

    private static IReadOnlyList<ISystemModel> SelectModels(ModelCatalog catalog, string? modelNames)
    {
        if (string.IsNullOrWhiteSpace(modelNames))
        {
            return catalog.All;
        }

        var selectedModels = new List<ISystemModel>();
        foreach (var modelName in modelNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var model = catalog.FindByName(modelName)
                ?? throw new InvalidOperationException($"Unknown model '{modelName}'. Run 'list-models' to see available models.");

            selectedModels.Add(model);
        }

        return selectedModels;
    }

    private static IReadOnlyList<string>? ParseModelNames(string? modelNames)
    {
        return string.IsNullOrWhiteSpace(modelNames)
            ? null
            : modelNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string ResolveOutputDirectory(string? outputDirectory, string scenarioName, int seed)
    {
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            return Path.GetFullPath(outputDirectory);
        }

        var slug = Slugify(scenarioName);
        var path = Path.Combine("runs", $"{slug}-{seed}");
        Directory.CreateDirectory(path);
        return Path.GetFullPath(path);
    }

    private static string ResolveSweepOutputDirectory(string? outputDirectory, string scenarioName, int seed)
    {
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            return Path.GetFullPath(outputDirectory);
        }

        var slug = Slugify(scenarioName);
        var path = Path.Combine("runs", $"{slug}-{seed}-sweep");
        Directory.CreateDirectory(path);
        return Path.GetFullPath(path);
    }

    private static string ResolveStressOutputDirectory(string? outputDirectory, string? gridName)
    {
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            return Path.GetFullPath(outputDirectory);
        }

        var slug = Slugify(string.IsNullOrWhiteSpace(gridName) ? "stress" : gridName);
        var path = Path.Combine("runs", $"{slug}-stress");
        Directory.CreateDirectory(path);
        return Path.GetFullPath(path);
    }

    private static void WriteRunBundle(
        string outputDirectory,
        SimulationRunResult result,
        ScenarioDefinition scenario,
        IReadOnlyList<ISystemModel> models,
        IReadOnlyList<IMetric> metrics,
        IReadOnlyDictionary<string, double> parameters)
    {
        WriteJson(Path.Combine(outputDirectory, "config.json"), new
        {
            scenario,
            result.Seed,
            result.Ticks,
            models = models.Select(model => new
            {
                model.Name,
                model.Version
            }),
            metrics = metrics.Select(metric => metric.Name),
            parameters,
            aiAnalysis = AiAnalysisUsage.NotUsed()
        });

        WriteMetricsCsv(Path.Combine(outputDirectory, "metrics.csv"), result);
        WriteEventsJsonl(Path.Combine(outputDirectory, "events.jsonl"), result);
        WriteCitizensCsv(Path.Combine(outputDirectory, "citizens-final.csv"), result);
        WriteJson(Path.Combine(outputDirectory, "summary.json"), SimulationRunSummary.Create(result));
    }

    private static void WriteMetricsCsv(string path, SimulationRunResult result)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("model,tick,metric,value,unit");

        foreach (var model in result.ModelResults)
        {
            foreach (var metric in model.Metrics)
            {
                writer.WriteLine(string.Join(',',
                    Csv(model.ModelName),
                    metric.Tick.ToString(CultureInfo.InvariantCulture),
                    Csv(metric.Name),
                    metric.Value.ToString(CultureInfo.InvariantCulture),
                    Csv(metric.Unit)));
            }
        }
    }

    private static void WriteSweepMetricsCsv(string path, IReadOnlyList<SweepRunReport> sweepRuns)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("sweepRun,runDirectory,parameters,model,metric,value,unit");

        foreach (var run in sweepRuns)
        {
            var parameters = FormatParameters(run.Parameters);
            foreach (var metric in run.FinalMetrics)
            {
                writer.WriteLine(string.Join(',',
                    run.RunIndex.ToString(CultureInfo.InvariantCulture),
                    Csv(run.Directory ?? ""),
                    Csv(parameters),
                    Csv(metric.Model),
                    Csv(metric.Name),
                    metric.Value.ToString(CultureInfo.InvariantCulture),
                    Csv(metric.Unit)));
            }
        }
    }

    private static void WriteStressMetricsCsv(string path, IReadOnlyList<StressSweepRunResult> stressRuns)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("stressRun,runDirectory,scenario,seed,ticks,model,parameters,metric,value,unit");

        foreach (var run in stressRuns)
        {
            var parameters = FormatParameters(run.Parameters);
            foreach (var metric in run.FinalMetrics)
            {
                writer.WriteLine(string.Join(',',
                    run.RunIndex.ToString(CultureInfo.InvariantCulture),
                    Csv(run.Directory ?? ""),
                    Csv(run.ScenarioName),
                    run.Seed.ToString(CultureInfo.InvariantCulture),
                    run.Ticks.ToString(CultureInfo.InvariantCulture),
                    Csv(run.Model),
                    Csv(parameters),
                    Csv(metric.Name),
                    metric.Value.ToString(CultureInfo.InvariantCulture),
                    Csv(metric.Unit)));
            }
        }
    }

    private static void WriteEventsJsonl(string path, SimulationRunResult result)
    {
        using var writer = new StreamWriter(path);

        foreach (var model in result.ModelResults)
        {
            foreach (var simEvent in model.Events)
            {
                writer.WriteLine(JsonSerializer.Serialize(new
                {
                    model = model.ModelName,
                    simEvent.Tick,
                    simEvent.Type,
                    simEvent.Description,
                    simEvent.Data
                }));
            }
        }
    }

    private static void WriteCitizensCsv(string path, SimulationRunResult result)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("model,citizenId,foodNeed,healthNeed,housingNeed,wealth,socialPower,trustInSystem,vulnerability");

        foreach (var model in result.ModelResults)
        {
            foreach (var citizen in model.World.Population.Citizens)
            {
                writer.WriteLine(string.Join(',',
                    Csv(model.ModelName),
                    Csv(citizen.Id.ToString()),
                    citizen.FoodNeed.ToString(CultureInfo.InvariantCulture),
                    citizen.HealthNeed.ToString(CultureInfo.InvariantCulture),
                    citizen.HousingNeed.ToString(CultureInfo.InvariantCulture),
                    citizen.Wealth.ToString(CultureInfo.InvariantCulture),
                    citizen.SocialPower.ToString(CultureInfo.InvariantCulture),
                    citizen.TrustInSystem.ToString(CultureInfo.InvariantCulture),
                    citizen.Vulnerability.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }

    private static void WriteJson(string path, object value)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions));
    }

    private static void WriteAiAnalysisUsage(string outputDirectory)
    {
        WriteJson(Path.Combine(outputDirectory, "ai-analysis.json"), AiAnalysisUsage.NotUsed());
    }

    private static string ReadSummaryBundleDirectory(string[] args)
    {
        var bundleDirectory = ReadOption(args, "--bundle");
        if (string.IsNullOrWhiteSpace(bundleDirectory))
        {
            throw new InvalidOperationException("The summary command requires --bundle <directory>.");
        }

        return Path.GetFullPath(bundleDirectory);
    }

    private static string? ReadOption(string[] args, string option)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new InvalidOperationException($"Option '{option}' requires a value.");
            }

            return args[index + 1];
        }

        return null;
    }

    private static IReadOnlyList<string> SelectAssumptions(IModelCatalog modelCatalog, IReadOnlyList<string> modelNames)
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

    private static string FormatParameters(IReadOnlyDictionary<string, double> parameters)
    {
        return string.Join(';', parameters
            .OrderBy(parameter => parameter.Key, StringComparer.OrdinalIgnoreCase)
            .Select(parameter => $"{parameter.Key}={parameter.Value.ToString(CultureInfo.InvariantCulture)}"));
    }

    private static string Csv(string value)
    {
        return value.Contains('"') || value.Contains(',') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private static string Slugify(string value)
    {
        var characters = value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        return string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsHelp(string value)
    {
        return value is "-h" or "--help" or "help";
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        WriteHelp();
        return 1;
    }

    private static void WriteHelp()
    {
        Console.WriteLine("""
        PolityKit simulation CLI

        Usage:
          PolityKit.Sim.Cli run [options]
          PolityKit.Sim.Cli sweep [options]
          PolityKit.Sim.Cli stress [options]
          PolityKit.Sim.Cli summary --bundle <directory> [--provider fake] [--out <file>]
          PolityKit.Sim.Cli list-models

        Run options:
          --scenario <name-or-file>  Scenario name or JSON file. Default: village-food-crisis
          --models <names>           Comma-separated model names, preset IDs, or preset:<id>. Default: all
          --seed <number[,number]>    Override scenario seed. May be repeated for stress
          --ticks <number>           Override scenario tick count
          --out <directory>          Output directory. Default: runs/<scenario>-<seed>
          --param <key=value>        Model parameter override. May be repeated
          --sweep <key=v1,v2>        Sweep parameter values. May be repeated
          --failure <criterion>      Failure criterion, e.g. "Needs Met<0.5" or "Severe Failures>=10%"
          --grid-name <name>          Optional stress parameter grid name
          --max-runs <number>         Optional stress run limit. Default: 512

        Examples:
          PolityKit.Sim.Cli run
          PolityKit.Sim.Cli run --models need-based-allocation,market-based-allocation --seed 12345 --ticks 120
          PolityKit.Sim.Cli run --models preset:regulated-market,participatory-commons
          PolityKit.Sim.Cli run --scenario examples/village-food-crisis.json --out runs/village-food-crisis-12345
          PolityKit.Sim.Cli sweep --models need-based-allocation --sweep needPriorityWeight=0.75,1.0,1.25
          PolityKit.Sim.Cli stress --scenario village-food-crisis --seed 111,222 --models need-based-allocation,market-based-allocation --sweep needPriorityWeight=0.75,1.0
          PolityKit.Sim.Cli summary --bundle runs/village-food-crisis-12345 --provider fake
        """);
    }

    private sealed class RunBundleConfig
    {
        public IReadOnlyList<RunBundleModelConfig> Models { get; init; } = [];

        public IReadOnlyDictionary<string, double> Parameters { get; init; } = new Dictionary<string, double>();
    }

    private sealed class RunBundleModelConfig
    {
        public string Name { get; init; } = "";
    }
}

internal sealed class CliOptions
{
    public string? Scenario { get; private init; }

    public IReadOnlyList<string> Scenarios { get; private init; } = [];

    public string? Models { get; private init; }

    public int? Seed { get; private init; }

    public IReadOnlyList<int> Seeds { get; private init; } = [];

    public int? Ticks { get; private init; }

    public string? OutputDirectory { get; private init; }

    public bool ShowHelp { get; private init; }

    public string? GridName { get; private init; }

    public int? MaxRuns { get; private init; }

    public IReadOnlyList<FailureCriterion>? FailureCriteria { get; private init; }

    public IReadOnlyDictionary<string, double> Parameters { get; private init; } = new Dictionary<string, double>();

    public IReadOnlyDictionary<string, IReadOnlyList<double>> Sweeps { get; private init; } =
        new Dictionary<string, IReadOnlyList<double>>();

    public static CliOptions Parse(string[] args)
    {
        var parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var sweeps = new Dictionary<string, IReadOnlyList<double>>(StringComparer.OrdinalIgnoreCase);
        var scenarios = new List<string>();
        string? models = null;
        var seeds = new List<int>();
        int? ticks = null;
        string? outputDirectory = null;
        var showHelp = false;
        string? gridName = null;
        int? maxRuns = null;
        var failureCriteria = new List<FailureCriterion>();

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
                case "--scenario":
                    scenarios.Add(ReadRequiredValue(args, ref index, "--scenario"));
                    break;
                case "--models":
                    models = ReadRequiredValue(args, ref index, "--models");
                    break;
                case "--seed":
                    seeds.AddRange(ReadIntList(ReadRequiredValue(args, ref index, "--seed"), "--seed"));
                    break;
                case "--ticks":
                    ticks = int.Parse(ReadRequiredValue(args, ref index, "--ticks"), CultureInfo.InvariantCulture);
                    break;
                case "--out":
                    outputDirectory = ReadRequiredValue(args, ref index, "--out");
                    break;
                case "--param":
                    var parameter = ReadParameter(ReadRequiredValue(args, ref index, "--param"));
                    parameters[parameter.Key] = parameter.Value;
                    break;
                case "--sweep":
                    var sweep = ReadSweep(ReadRequiredValue(args, ref index, "--sweep"));
                    sweeps[sweep.Key] = sweep.Value;
                    break;
                case "--failure":
                    failureCriteria.Add(ReadFailureCriterion(ReadRequiredValue(args, ref index, "--failure")));
                    break;
                case "--grid-name":
                    gridName = ReadRequiredValue(args, ref index, "--grid-name");
                    break;
                case "--max-runs":
                    maxRuns = int.Parse(ReadRequiredValue(args, ref index, "--max-runs"), CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown option '{args[index]}'.");
            }
        }

        return new CliOptions
        {
            Scenario = scenarios.LastOrDefault(),
            Scenarios = scenarios,
            Models = models,
            Seed = seeds.Count > 0 ? seeds[^1] : null,
            Seeds = seeds,
            Ticks = ticks,
            OutputDirectory = outputDirectory,
            ShowHelp = showHelp,
            GridName = gridName,
            MaxRuns = maxRuns,
            FailureCriteria = failureCriteria.Count == 0 ? null : failureCriteria,
            Parameters = parameters,
            Sweeps = sweeps
        };
    }

    private static string ReadRequiredValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new InvalidOperationException($"Option '{option}' requires a value.");
        }

        index++;
        return args[index];
    }

    private static KeyValuePair<string, double> ReadParameter(string value)
    {
        var parts = value.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
        {
            throw new InvalidOperationException("Parameter overrides must use key=value format.");
        }

        return new KeyValuePair<string, double>(
            parts[0],
            double.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    private static IReadOnlyList<int> ReadIntList(string value, string option)
    {
        var values = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => int.Parse(item, CultureInfo.InvariantCulture))
            .ToArray();

        if (values.Length == 0)
        {
            throw new InvalidOperationException($"Option '{option}' requires at least one value.");
        }

        return values;
    }

    private static KeyValuePair<string, IReadOnlyList<double>> ReadSweep(string value)
    {
        var parts = value.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
        {
            throw new InvalidOperationException("Sweep parameters must use key=v1,v2 format.");
        }

        var values = parts[1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => double.Parse(item, CultureInfo.InvariantCulture))
            .ToArray();
        if (values.Length == 0)
        {
            throw new InvalidOperationException($"Sweep parameter '{parts[0]}' must include at least one value.");
        }

        return new KeyValuePair<string, IReadOnlyList<double>>(parts[0], values);
    }

    private static FailureCriterion ReadFailureCriterion(string value)
    {
        var operators = new[]
        {
            (Text: "<=", Operator: FailureOperator.LessThanOrEqual),
            (Text: ">=", Operator: FailureOperator.GreaterThanOrEqual),
            (Text: "<", Operator: FailureOperator.LessThan),
            (Text: ">", Operator: FailureOperator.GreaterThan)
        };

        foreach (var candidate in operators)
        {
            var operatorIndex = value.IndexOf(candidate.Text, StringComparison.Ordinal);
            if (operatorIndex < 0)
            {
                continue;
            }

            var metric = value[..operatorIndex].Trim();
            var thresholdText = value[(operatorIndex + candidate.Text.Length)..].Trim();
            var recoveryTicks = 1;
            var recoverySeparator = thresholdText.LastIndexOf(':');
            if (recoverySeparator >= 0)
            {
                recoveryTicks = int.Parse(thresholdText[(recoverySeparator + 1)..], CultureInfo.InvariantCulture);
                thresholdText = thresholdText[..recoverySeparator].Trim();
            }

            if (string.IsNullOrWhiteSpace(metric) || string.IsNullOrWhiteSpace(thresholdText))
            {
                throw new InvalidOperationException("Failure criteria must use metric<value, metric<=value, metric>value, or metric>=value format.");
            }

            var thresholdKind = thresholdText.EndsWith('%')
                ? FailureThresholdKind.PopulationShare
                : FailureThresholdKind.Absolute;
            if (thresholdKind == FailureThresholdKind.PopulationShare)
            {
                thresholdText = thresholdText[..^1];
            }

            var threshold = double.Parse(thresholdText, CultureInfo.InvariantCulture);
            if (thresholdKind == FailureThresholdKind.PopulationShare)
            {
                threshold /= 100;
            }

            return new FailureCriterion(metric, candidate.Operator, threshold, thresholdKind, recoveryTicks);
        }

        throw new InvalidOperationException("Failure criteria must use metric<value, metric<=value, metric>value, or metric>=value format.");
    }
}
