using System.Globalization;
using System.Text.Json;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Engine;
using PolityKit.Sim.Metrics;
using PolityKit.Sim.Models;

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

        var scenario = LoadScenario(options.Scenario);
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

    private static ScenarioDefinition LoadScenario(string? scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario) || IsBuiltInVillageFoodCrisis(scenario))
        {
            return BuiltInScenarios.VillageFoodCrisis();
        }

        if (!File.Exists(scenario))
        {
            throw new FileNotFoundException($"Scenario file '{scenario}' was not found.");
        }

        var loadedScenario = JsonSerializer.Deserialize<ScenarioDefinition>(File.ReadAllText(scenario), JsonOptions);
        return loadedScenario ?? throw new InvalidOperationException($"Scenario file '{scenario}' could not be read.");
    }

    private static bool IsBuiltInVillageFoodCrisis(string value)
    {
        return string.Equals(value, "village-food-crisis", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Village Food Crisis", StringComparison.OrdinalIgnoreCase);
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
        WriteJson(Path.Combine(outputDirectory, "summary.json"), CreateSummary(result));
    }

    private static object CreateSummary(SimulationRunResult result)
    {
        return new
        {
            result.ScenarioName,
            result.Seed,
            result.Ticks,
            Models = result.ModelResults.Select(model => new
            {
                model.ModelName,
                model.ModelVersion,
                EventCount = model.Events.Count,
                FinalMetrics = model.Metrics
                    .GroupBy(metric => metric.Name)
                    .Select(group => group.OrderByDescending(metric => metric.Tick).First())
                    .OrderBy(metric => metric.Name)
                    .Select(metric => new
                    {
                        metric.Name,
                        metric.Tick,
                        metric.Value
                    })
            })
        };
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
          PolityKit.Sim.Cli list-models

        Run options:
          --scenario <name-or-file>  Scenario name or JSON file. Default: village-food-crisis
          --models <names>           Comma-separated model names. Default: all
          --seed <number>            Override scenario seed
          --ticks <number>           Override scenario tick count
          --out <directory>          Output directory. Default: runs/<scenario>-<seed>
          --param <key=value>        Model parameter override. May be repeated

        Examples:
          PolityKit.Sim.Cli run
          PolityKit.Sim.Cli run --models need-based-allocation,market-based-allocation --seed 12345 --ticks 120
          PolityKit.Sim.Cli run --scenario examples/village-food-crisis.json --out runs/village-food-crisis-12345
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

    public static CliOptions Parse(string[] args)
    {
        var parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
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
            Parameters = parameters
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
}

internal static class ScenarioExtensions
{
    public static ScenarioDefinition WithSeed(this ScenarioDefinition scenario, int seed)
    {
        return new ScenarioDefinition
        {
            Name = scenario.Name,
            Seed = seed,
            Ticks = scenario.Ticks,
            InitialPopulation = scenario.InitialPopulation,
            InitialResources = scenario.InitialResources,
            Shocks = scenario.Shocks
        };
    }

    public static ScenarioDefinition WithTicks(this ScenarioDefinition scenario, int ticks)
    {
        return new ScenarioDefinition
        {
            Name = scenario.Name,
            Seed = scenario.Seed,
            Ticks = ticks,
            InitialPopulation = scenario.InitialPopulation,
            InitialResources = scenario.InitialResources,
            Shocks = scenario.Shocks
        };
    }
}

internal static class BuiltInScenarios
{
    public static ScenarioDefinition VillageFoodCrisis()
    {
        return new ScenarioDefinition
        {
            Name = "Village Food Crisis",
            Seed = 12345,
            Ticks = 120,
            InitialPopulation = 500,
            InitialResources = new ResourcePool
            {
                Food = 800,
                Medicine = 120,
                Housing = 450,
                AdminCapacity = 80,
                ProductionCapacity = 100
            },
            Shocks =
            [
                new ShockDefinition
                {
                    Tick = 20,
                    Type = "CropFailure",
                    Severity = 0.4
                },
                new ShockDefinition
                {
                    Tick = 45,
                    Type = "AdministrativeOverload",
                    Severity = 0.3
                }
            ]
        };
    }
}
