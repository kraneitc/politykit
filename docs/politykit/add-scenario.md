# How To Add A Scenario

Scenarios define starting conditions and scheduled shocks. They let every model face the same stress pattern, which keeps comparisons meaningful.

Use this guide when adding JSON files under `examples/`.

For the full field reference, see [scenarios.md](scenarios.md). For the formal schema, see [scenario.schema.json](scenario.schema.json).

## Add The Scenario File

Copy an existing example from `examples/` and rename it with a short kebab-case filename:

```text
examples/water-shortage.json
```

Use this basic shape:

```json
{
  "name": "Water Shortage",
  "seed": 20260614,
  "ticks": 90,
  "initialPopulation": 500,
  "initialResources": {
    "food": 750,
    "medicine": 120,
    "housing": 450,
    "adminCapacity": 80,
    "productionCapacity": 100
  },
  "shocks": [
    {
      "tick": 20,
      "type": "CropFailure",
      "severity": 0.35
    },
    {
      "tick": 45,
      "type": "AdministrativeOverload",
      "severity": 0.25
    }
  ]
}
```

## Choose A Clear Purpose

A useful scenario should answer a focused question:

- What happens when food supply drops before institutions are stressed?
- Which models recover after housing loss?
- Does corruption degrade trust before material needs fail?
- Which model is sensitive to administrative capacity?

Use the `name`, resource levels, shock timing, and shock severity to make that question visible.

## Validation Rules

Scenario validation enforces:

- `name` is required and cannot be blank.
- `ticks` must be greater than `0`.
- `initialPopulation` cannot be negative.
- Resource values cannot be negative.
- Shock ticks must be within the run window.
- Shock types cannot be blank.
- Shock severity must be between `0` and `1`.

New scenario JSON should use camelCase field names even though the loader is case-insensitive.

## Supported Built-In Shock Types

| Type | Current effect |
|---|---|
| `CropFailure` | Reduces food production and current food supply. |
| `MedicineShortage` | Reduces medicine supply multiplier and current medicine supply. |
| `HousingLoss` | Reduces housing availability and current housing supply. |
| `AdministrativeOverload` | Increases administrative load and reduces admin capacity. |
| `AdminLoss` | Same current behavior as `AdministrativeOverload`. |
| `CorruptionSpike` | Increases institutional corruption and reduces institutional trust. |

Custom nonblank shock types are allowed. The default shock handler applies a small stability reduction based on severity, so custom shocks are useful for naming stress patterns before a dedicated handler exists.

## Run The Scenario

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run \
  --scenario examples/water-shortage.json \
  --seed 20260614 \
  --out runs/water-shortage-check
```

On Windows PowerShell:

```powershell
dotnet run --project src/PolityKit.Sim.Cli -- run `
  --scenario examples/water-shortage.json `
  --seed 20260614 `
  --out runs/water-shortage-check
```

Inspect:

- `summary.json` for final metrics and notable changes.
- `metrics.csv` for metric movement over time.
- `events.jsonl` for shock, allocation, administrative, trust, and severe failure events.

## Add Tests Or Update Existing Expectations

Example scenarios are covered by scenario tests under `tests/PolityKit.Sim.Tests/Scenarios/`.

Before opening a change, run:

```bash
dotnet test PolityKit.slnx
```

If the scenario introduces a new field or shock behavior, add targeted tests for the loader, validator, schema, or shock handler.

## Scenario Review Checklist

- The filename is kebab-case.
- The `name` is human-readable.
- The `seed` is fixed.
- The run length gives the scenario time to fail or recover.
- Starting resources create intentional pressure.
- Shock timing is inside the run window.
- Shock severity is strong enough to observe but not so strong that every model collapses immediately unless collapse is the point.
- CLI output is reproducible with the documented seed.

