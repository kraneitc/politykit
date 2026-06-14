namespace PolityKit.Sim.Api.Contracts;

public sealed class ScenarioResponse
{
    public string Name { get; init; } = "";

    public string Slug { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public int InitialPopulation { get; init; }

    public IReadOnlyList<ShockResponse> Shocks { get; init; } = [];
}

public sealed class ShockResponse
{
    public int Tick { get; init; }

    public string Type { get; init; } = "";

    public double Severity { get; init; }
}
