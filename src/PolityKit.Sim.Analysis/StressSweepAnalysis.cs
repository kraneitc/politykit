namespace PolityKit.Sim.Analysis;

public static class StressSweepAnalysis
{
    public const int DefaultMaxRuns = 512;

    public static StressSweepPlan BuildPlan(StressSweepRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var maxRuns = request.MaxRuns ?? DefaultMaxRuns;
        var scenarios = NormalizeNames(request.Scenarios, "At least one scenario is required.", "Scenario names cannot be blank.");
        var seeds = NormalizeSeeds(request.Seeds);
        var models = NormalizeNames(request.Models, "At least one model is required.", "Model names cannot be blank.");
        var parameterCombinations = BuildParameterCombinations(request.Parameters, request.Sweep, maxRuns);
        var totalRuns = checked(scenarios.Count * seeds.Count * models.Count * parameterCombinations.Count);

        if (totalRuns > maxRuns)
        {
            throw new InvalidOperationException($"Stress sweep would create {totalRuns} runs; the maximum is {maxRuns}.");
        }

        var runs = new List<StressSweepRunPlan>(totalRuns);
        var runIndex = 1;
        foreach (var scenario in scenarios)
        {
            foreach (var seed in seeds)
            {
                foreach (var model in models)
                {
                    foreach (var parameters in parameterCombinations)
                    {
                        runs.Add(new StressSweepRunPlan(
                            runIndex,
                            scenario,
                            seed,
                            model,
                            parameters));
                        runIndex++;
                    }
                }
            }
        }

        return new StressSweepPlan(
            request.GridName,
            scenarios,
            seeds,
            models,
            request.Parameters ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
            request.Sweep is null || request.Sweep.Count == 0
                ? new Dictionary<string, IReadOnlyList<double>>(StringComparer.OrdinalIgnoreCase)
                : SweepAnalysis.NormalizeSweep(request.Sweep, maxRuns),
            runs);
    }

    private static IReadOnlyList<string> NormalizeNames(
        IReadOnlyList<string>? values,
        string emptyMessage,
        string blankMessage)
    {
        if (values is null || values.Count == 0)
        {
            throw new InvalidOperationException(emptyMessage);
        }

        var normalized = new List<string>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(blankMessage);
            }

            normalized.Add(value.Trim());
        }

        return normalized;
    }

    private static IReadOnlyList<int> NormalizeSeeds(IReadOnlyList<int>? seeds)
    {
        if (seeds is null || seeds.Count == 0)
        {
            throw new InvalidOperationException("At least one seed is required.");
        }

        return seeds.ToArray();
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, double>> BuildParameterCombinations(
        IReadOnlyDictionary<string, double>? parameters,
        IReadOnlyDictionary<string, IReadOnlyList<double>>? sweep,
        int maxRuns)
    {
        var baseParameters = parameters ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        return sweep is null || sweep.Count == 0
            ? [new Dictionary<string, double>(baseParameters, StringComparer.OrdinalIgnoreCase)]
            : SweepAnalysis.BuildParameterCombinations(baseParameters, sweep, maxRuns);
    }
}

public sealed class StressSweepRequest
{
    public string? GridName { get; init; }

    public IReadOnlyList<string>? Scenarios { get; init; }

    public IReadOnlyList<int>? Seeds { get; init; }

    public IReadOnlyList<string>? Models { get; init; }

    public IReadOnlyDictionary<string, double>? Parameters { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<double>>? Sweep { get; init; }

    public int? MaxRuns { get; init; }
}

public sealed record StressSweepPlan(
    string? GridName,
    IReadOnlyList<string> Scenarios,
    IReadOnlyList<int> Seeds,
    IReadOnlyList<string> Models,
    IReadOnlyDictionary<string, double> BaseParameters,
    IReadOnlyDictionary<string, IReadOnlyList<double>> Sweep,
    IReadOnlyList<StressSweepRunPlan> Runs);

public sealed record StressSweepRunPlan(
    int RunIndex,
    string Scenario,
    int Seed,
    string Model,
    IReadOnlyDictionary<string, double> Parameters);

public sealed record StressSweepResult(
    string? GridName,
    IReadOnlyList<string> Scenarios,
    IReadOnlyList<int> Seeds,
    IReadOnlyList<string> Models,
    IReadOnlyDictionary<string, double> BaseParameters,
    IReadOnlyDictionary<string, IReadOnlyList<double>> Sweep,
    int RunCount,
    IReadOnlyList<StressSweepRunResult> Runs,
    IReadOnlyList<SweepBestWorstReport> BestWorst);

public sealed record StressSweepRunResult(
    int RunIndex,
    string? Directory,
    Guid? RunId,
    string ScenarioName,
    int Seed,
    int Ticks,
    string Model,
    IReadOnlyDictionary<string, double> Parameters,
    IReadOnlyList<SweepMetricReport> FinalMetrics);
