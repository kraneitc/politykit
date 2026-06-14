using System.Globalization;
using System.Text.Json;
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
        WriteIndented = true
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

        Console.WriteLine($"Wrote run output to {outputDirectory}");
        Console.WriteLine($"Scenario: {result.ScenarioName}");
        Console.WriteLine($"Seed: {result.Seed}");
        Console.WriteLine($"Ticks: {result.Ticks}");
        Console.WriteLine($"Models: {string.Join(", ", result.ModelResults.Select(model => model.ModelName))}");

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

        if (options.Sweeps.Count == 0)
        {
            throw new InvalidOperationException("At least one --sweep parameter is required.");
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
        var combinations = BuildParameterCombinations(options.Parameters, options.Sweeps);
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
                SelectFinalMetrics(result)));
        }

        var bestWorst = BuildBestWorst(sweepRuns);
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
            sweep = options.Sweeps,
            runCount = sweepRuns.Count,
            runs = sweepRuns.Select(run => new
            {
                run.RunIndex,
                run.Directory,
                run.Parameters,
                finalMetrics = run.FinalMetrics
            }),
            bestWorst
        });

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
            parameters
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
                    Csv(run.Directory),
                    Csv(parameters),
                    Csv(metric.Model),
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

    private static IReadOnlyList<Dictionary<string, double>> BuildParameterCombinations(
        IReadOnlyDictionary<string, double> baseParameters,
        IReadOnlyDictionary<string, IReadOnlyList<double>> sweeps)
    {
        var combinations = new List<Dictionary<string, double>>
        {
            new(baseParameters, StringComparer.OrdinalIgnoreCase)
        };

        foreach (var (name, values) in sweeps.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
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

        if (combinations.Count > 256)
        {
            throw new InvalidOperationException($"Sweep would create {combinations.Count} runs; the maximum is 256.");
        }

        return combinations;
    }

    private static IReadOnlyList<SweepMetricReport> SelectFinalMetrics(SimulationRunResult result)
    {
        return result.ModelResults
            .SelectMany(model => model.Metrics
                .GroupBy(metric => metric.Name)
                .Select(group => group.OrderByDescending(metric => metric.Tick).First())
                .OrderBy(metric => metric.Name)
                .Select(metric => new SweepMetricReport(
                    model.ModelName,
                    metric.Name,
                    metric.Value,
                    metric.Unit)))
            .ToArray();
    }

    private static IReadOnlyList<SweepBestWorstReport> BuildBestWorst(IReadOnlyList<SweepRunReport> sweepRuns)
    {
        return sweepRuns
            .SelectMany(run => run.FinalMetrics.Select(metric => new
            {
                Run = run,
                Metric = metric
            }))
            .GroupBy(item => new
            {
                item.Metric.Model,
                item.Metric.Name,
                item.Metric.Unit
            })
            .OrderBy(group => group.Key.Model)
            .ThenBy(group => group.Key.Name)
            .Select(group =>
            {
                var higherIsBetter = HigherIsBetter(group.Key.Name);
                var best = higherIsBetter
                    ? group.MaxBy(item => item.Metric.Value)!
                    : group.MinBy(item => item.Metric.Value)!;
                var worst = higherIsBetter
                    ? group.MinBy(item => item.Metric.Value)!
                    : group.MaxBy(item => item.Metric.Value)!;
                return new SweepBestWorstReport(
                    group.Key.Model,
                    group.Key.Name,
                    group.Key.Unit,
                    higherIsBetter ? "higher" : "lower",
                    ToSweepMetricRun(best.Run, best.Metric),
                    ToSweepMetricRun(worst.Run, worst.Metric));
            })
            .ToArray();
    }

    private static bool HigherIsBetter(string metricName)
    {
        return metricName is "Needs Met" or "Trust";
    }

    private static SweepMetricRunReport ToSweepMetricRun(SweepRunReport run, SweepMetricReport metric)
    {
        return new SweepMetricRunReport(
            run.RunIndex,
            run.Directory,
            metric.Value,
            run.Parameters);
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
          PolityKit.Sim.Cli list-models

        Run options:
          --scenario <name-or-file>  Scenario name or JSON file. Default: village-food-crisis
          --models <names>           Comma-separated model names. Default: all
          --seed <number>            Override scenario seed
          --ticks <number>           Override scenario tick count
          --out <directory>          Output directory. Default: runs/<scenario>-<seed>
          --param <key=value>        Model parameter override. May be repeated
          --sweep <key=v1,v2>        Sweep parameter values. May be repeated

        Examples:
          PolityKit.Sim.Cli run
          PolityKit.Sim.Cli run --models need-based-allocation,market-based-allocation --seed 12345 --ticks 120
          PolityKit.Sim.Cli run --scenario examples/village-food-crisis.json --out runs/village-food-crisis-12345
          PolityKit.Sim.Cli sweep --models need-based-allocation --sweep needPriorityWeight=0.75,1.0,1.25
        """);
    }
}

internal sealed class CliOptions
{
    public string? Scenario { get; private init; }

    public string? Models { get; private init; }

    public int? Seed { get; private init; }

    public int? Ticks { get; private init; }

    public string? OutputDirectory { get; private init; }

    public bool ShowHelp { get; private init; }

    public IReadOnlyDictionary<string, double> Parameters { get; private init; } = new Dictionary<string, double>();

    public IReadOnlyDictionary<string, IReadOnlyList<double>> Sweeps { get; private init; } =
        new Dictionary<string, IReadOnlyList<double>>();

    public static CliOptions Parse(string[] args)
    {
        var parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var sweeps = new Dictionary<string, IReadOnlyList<double>>(StringComparer.OrdinalIgnoreCase);
        string? scenario = null;
        string? models = null;
        int? seed = null;
        int? ticks = null;
        string? outputDirectory = null;
        var showHelp = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
                case "--scenario":
                    scenario = ReadRequiredValue(args, ref index, "--scenario");
                    break;
                case "--models":
                    models = ReadRequiredValue(args, ref index, "--models");
                    break;
                case "--seed":
                    seed = int.Parse(ReadRequiredValue(args, ref index, "--seed"), CultureInfo.InvariantCulture);
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
                default:
                    throw new InvalidOperationException($"Unknown option '{args[index]}'.");
            }
        }

        return new CliOptions
        {
            Scenario = scenario,
            Models = models,
            Seed = seed,
            Ticks = ticks,
            OutputDirectory = outputDirectory,
            ShowHelp = showHelp,
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
}

internal sealed record SweepRunReport(
    int RunIndex,
    string Directory,
    IReadOnlyDictionary<string, double> Parameters,
    IReadOnlyList<SweepMetricReport> FinalMetrics);

internal sealed record SweepMetricReport(
    string Model,
    string Name,
    double Value,
    string Unit);

internal sealed record SweepBestWorstReport(
    string Model,
    string Metric,
    string Unit,
    string BestDirection,
    SweepMetricRunReport Best,
    SweepMetricRunReport Worst);

internal sealed record SweepMetricRunReport(
    int RunIndex,
    string Directory,
    double Value,
    IReadOnlyDictionary<string, double> Parameters);
