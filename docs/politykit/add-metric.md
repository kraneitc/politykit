# How To Add A Metric

Metrics turn a world state and event stream into values that can be compared across models and ticks.

Use this guide when adding a new implementation under `src/PolityKit.Sim.Metrics/`.

## Metric Contract

Metrics implement `IMetric` from `PolityKit.Sim.Core.Metrics`:

```csharp
public interface IMetric
{
    string Name { get; }

    double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events);
}
```

The engine records metric values for each model on each tick. The CLI exports them to `metrics.csv`, and the API exposes them through run endpoints.

## Add The Metric Class

Create a new class in `src/PolityKit.Sim.Metrics/`.

```csharp
using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Metrics;

public sealed class AverageWealthMetric : IMetric
{
    public string Name => "Average Wealth";

    public double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Population.Count == 0)
        {
            return 0;
        }

        return world.Population.Citizens.Average(citizen => citizen.Wealth);
    }
}
```

If the metric uses events, validate `events` too:

```csharp
ArgumentNullException.ThrowIfNull(events);
```

## Register The Metric

Add the metric to `DefaultMetricSet.Create()`:

```csharp
public static IReadOnlyList<IMetric> Create()
{
    return
    [
        new NeedsMetMetric(),
        new InequalityMetric(),
        new TrustMetric(),
        new SevereFailuresMetric(),
        new AdministrativeLoadMetric(),
        new AverageWealthMetric()
    ];
}
```

After registration, the CLI run output, API run output, and `/api/metrics` endpoint include the metric.

## Metric Design Guidelines

Good metrics are:

- Deterministic for the same world and events.
- Comparable across models in the same scenario.
- Clear about what a higher or lower value means.
- Robust for empty populations or missing event types.
- Focused on one concept.

Avoid:

- Mutating `world` or `events`.
- Calling external services.
- Using wall-clock time.
- Returning `NaN` or infinity.
- Combining too many concepts into one score without documentation.

## Name And Units

The current metric contract exposes a `Name`; run output stores metric values as unitless unless a later formatter supplies a unit. Choose names that read clearly in CSV and JSON output.

Examples:

- `Needs Met`
- `Inequality`
- `Trust`
- `Severe Failures`
- `Administrative Load`

Prefer stable names. Changing a metric name changes CSV/API consumers.

## Add Tests

Add tests under `tests/PolityKit.Sim.Tests/Metrics/`.

Recommended coverage:

- Empty-world behavior.
- A representative low value.
- A representative high value.
- Event-sensitive behavior, if the metric reads events.
- Null guard behavior.
- Default metric catalog registration.

Run:

```bash
dotnet test PolityKit.slnx
```

## Try The Metric

Run a scenario:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run \
  --scenario examples/village-food-crisis.json \
  --seed 12345 \
  --ticks 60 \
  --out runs/metric-check
```

Check that the new metric appears in:

- `runs/metric-check/metrics.csv`
- `runs/metric-check/summary.json`
- `GET /api/metrics`
- `GET /api/runs/{id}/metrics`

