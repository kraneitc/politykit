namespace PolityKit.Sim.Api.Contracts;

public sealed class StressSweepRequest
{
    public string? GridName { get; init; }

    public IReadOnlyList<string>? Scenarios { get; init; }

    public IReadOnlyList<int>? Seeds { get; init; }

    public int? Ticks { get; init; }

    public IReadOnlyList<string>? Models { get; init; }

    public IReadOnlyDictionary<string, double>? Parameters { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<double>>? Sweep { get; init; }

    public int? MaxRuns { get; init; }
}
