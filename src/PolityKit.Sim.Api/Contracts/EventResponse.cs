namespace PolityKit.Sim.Api.Contracts;

public sealed class EventResponse
{
    public string Model { get; init; } = "";

    public int Tick { get; init; }

    public string Type { get; init; } = "";

    public string Description { get; init; } = "";

    public IReadOnlyDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();
}
