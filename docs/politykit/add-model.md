# How To Add A Model

Models decide what a simulated system does at each tick. The engine owns the shared world mechanics; a model owns its allocation priorities, policy changes, and institutional actions.

Use this guide when adding a new implementation under `src/PolityKit.Sim.Models/`.

## Model Contract

Models implement `ISystemModel` from `PolityKit.Sim.Core.Models`:

```csharp
public interface ISystemModel
{
    string Name { get; }

    string Version { get; }

    SystemDecision Decide(WorldState world, SystemContext context);
}
```

Most allocation models should inherit from `AllocationModelBase`, which already provides `Version`, basic allocation helpers, and shared need helpers.

## Add The Model Class

Create a new class in `src/PolityKit.Sim.Models/`.

```csharp
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Models;

public sealed class LotteryAllocation : AllocationModelBase
{
    public override string Name => "LotteryAllocation";

    public override ModelManifest Manifest { get; } = new()
    {
        Model = "LotteryAllocation",
        Version = "0.1.0",
        Description = "Allocates resources with equal priority across citizens.",
        Assumptions =
        [
            new ModelAssumption
            {
                Name = "basePriority",
                Default = 1.0,
                Description = "Base allocation priority assigned to every citizen."
            }
        ],
        KnownFailureModes =
        [
            "does not prioritize urgent need",
            "may leave vulnerable citizens under-provisioned"
        ]
    };

    public override SystemDecision Decide(WorldState world, Core.Simulation.SystemContext context)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);

        var basePriority = context.Parameters.GetValueOrDefault("basePriority", 1.0);
        var decision = new SystemDecision();

        foreach (var citizen in world.Population.Citizens)
        {
            decision.Allocations.AddRange(AllocateBasicNeeds(
                citizen,
                basePriority,
                "Assigned equal baseline priority."));
        }

        return decision;
    }
}
```

## Register The Model

Add the model to `DefaultModelSet.Create()`:

```csharp
public static IReadOnlyList<ISystemModel> Create()
{
    return
    [
        new NeedBasedAllocation(),
        new MarketBasedAllocation(),
        new HierarchyBasedAllocation(),
        new LotteryAllocation()
    ];
}
```

After registration, the CLI and API model catalogs can find it by exact name or generated kebab-case name:

```text
LotteryAllocation
lottery-allocation
```

## Write The Manifest Carefully

Every model should expose:

| Field | Guidance |
|---|---|
| `Model` | Match the model `Name`. |
| `Version` | Update when behavior or assumptions change. |
| `Description` | Say what decision rule the model uses. |
| `Assumptions` | List tunable weights and important fixed premises. |
| `GovernanceDimensions` | For composite governance presets, list each selected dimension, its value, assumption, parameters, and dimension-specific failure modes. Leave empty for ordinary baseline models. |
| `KnownFailureModes` | Name where this model is expected to degrade. |

If the model reads `context.Parameters`, each parameter should appear in the manifest with the same name and default value.

Use a baseline model when you are adding a new mechanical decision rule, such as a different allocation algorithm. Use a governance preset when you are composing existing governance dimensions into a named experimental bundle. Preset labels should describe simplified assumptions, not real-world systems. See [Governance presets](governance-presets.md) for interpretation boundaries and current preset conventions.

## Baseline Model Or Governance Preset?

Choose a baseline model when the change introduces a distinct decision rule that should stand on its own for direct comparison. Examples include a new allocation formula, a new institutional action strategy, or a model that intentionally changes how decisions are made at each tick.

Choose a governance preset when the change recombines existing governance dimensions into a named experimental bundle. A preset should make its dimensions, assumptions, and known failure modes visible in the manifest.

Avoid using preset names as real-world claims. A preset name is a compact handle for a simulation profile, not a verdict about a society or institution.

## Decision Guidelines

`Decide` receives a fresh `WorldState` for the model and the current `SystemContext`.

Inside `Decide`:

- Validate `world` and `context`.
- Read parameters with deterministic defaults.
- Return a new `SystemDecision`.
- Use `ResourceAllocation` priorities to express who should receive scarce resources first.
- Use `PolicyChange` or `InstitutionalAction` only when the model is intentionally changing institutional behavior.

Avoid:

- Mutating `world` directly from the model.
- Reading external state.
- Using unseeded randomness.
- Hiding model assumptions in magic constants.
- Depending on citizen iteration order unless that ordering is part of the model's declared behavior.

## Add Tests

Add tests under `tests/PolityKit.Sim.Tests/Models/`.

Recommended coverage:

- The default catalog includes the model.
- The catalog resolves the kebab-case name.
- The manifest has a model name, version, description, assumptions, and known failure modes.
- `Decide` rejects null inputs.
- Representative worlds produce expected allocation priorities, policy changes, or institutional actions.
- Parameter changes affect behavior in the intended direction.

Run:

```bash
dotnet test PolityKit.slnx
```

## Try The Model

Run it through the CLI:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run \
  --scenario examples/village-food-crisis.json \
  --models lottery-allocation \
  --seed 12345 \
  --ticks 60 \
  --out runs/lottery-allocation-check
```

Inspect:

- `summary.json` for final metrics and notable changes.
- `metrics.csv` for the metric series.
- `events.jsonl` for allocation and failure events.
