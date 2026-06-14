namespace PolityKit.Sim.Api.Contracts;

public sealed class ParameterSweepRequest
{
    public string? Scenario { get; init; }

    public int? Seed { get; init; }

    public int? Ticks { get; init; }

    public IReadOnlyList<string>? Models { get; init; }

    public IReadOnlyDictionary<string, double>? Parameters { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<double>>? Sweep { get; init; }
}
