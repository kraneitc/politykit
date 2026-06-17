using Charterfall.App.Models;

namespace Charterfall.App.Services;

public sealed class PlaceholderPolityKitRunClient : IPolityKitRunClient
{
    public Task<PrototypeRunRecord> CreatePlaceholderRunAsync(
        string label,
        CrisisCard crisis,
        CancellationToken cancellationToken = default)
    {
        var slug = label.ToLowerInvariant().Replace(' ', '-');
        var run = new PrototypeRunRecord(
            $"pending-{slug}-run",
            label,
            crisis.ScenarioId,
            crisis.Seed,
            crisis.Ticks,
            IsAuthoritative: false,
            "Integration pending: placeholder run, not deterministic PolityKit output");

        return Task.FromResult(run);
    }
}
