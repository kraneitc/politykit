namespace PolityKit.Sim.Api.Contracts;

public sealed class RerunRequest
{
    public int? Ticks { get; init; }

    public IReadOnlyList<string>? Models { get; init; }

    public IReadOnlyDictionary<string, double>? Parameters { get; init; }
}
