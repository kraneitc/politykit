# Scenario Format

PolityKit scenarios are JSON files that define the starting world and scheduled shocks for a simulation run.

The formal schema is [scenario.schema.json](scenario.schema.json). The examples in [../../examples](../../examples) use the same format.

## Basic Shape

```json
{
  "name": "Village Food Crisis",
  "seed": 12345,
  "ticks": 120,
  "initialPopulation": 500,
  "initialResources": {
    "food": 800,
    "medicine": 120,
    "housing": 450,
    "adminCapacity": 80,
    "productionCapacity": 100
  },
  "shocks": [
    {
      "tick": 20,
      "type": "CropFailure",
      "severity": 0.4
    }
  ]
}
```

## Fields

| Field | Required | Description |
|---|---:|---|
| `name` | Yes | Human-readable scenario name. Must not be blank. |
| `seed` | No | Deterministic seed used when a run does not override it. |
| `ticks` | Yes | Number of ticks to simulate. Must be greater than zero. |
| `initialPopulation` | Yes | Number of citizens generated at the start. Must not be negative. |
| `initialResources` | Yes | Starting resources available to each model run. |
| `shocks` | No | Scheduled disruptions applied at specific ticks. |

`initialResources` contains:

| Field | Description |
|---|---|
| `food` | Starting food supply. |
| `medicine` | Starting medicine supply. |
| `housing` | Starting housing supply. |
| `adminCapacity` | Starting administrative capacity. |
| `productionCapacity` | Starting production capacity. |

All resource values must be nonnegative integers.

## Shock Types

Each shock has:

| Field | Required | Description |
|---|---:|---|
| `tick` | Yes | Zero-based tick when the shock is applied. Must be within the scenario range. |
| `type` | Yes | Shock type. Must not be blank. |
| `severity` | Yes | Number from `0` to `1`, where `1` is maximum severity. |
| `parameters` | No | Optional object reserved for future or custom shock handlers. |

Built-in shock types:

| Type | Current effect |
|---|---|
| `CropFailure` | Reduces food production and current food supply. |
| `MedicineShortage` | Reduces medicine supply multiplier and current medicine supply. |
| `HousingLoss` | Reduces housing availability and current housing supply. |
| `AdministrativeOverload` | Increases administrative load and reduces admin capacity. |
| `AdminLoss` | Same current behavior as `AdministrativeOverload`. |
| `CorruptionSpike` | Increases institutional corruption and reduces institutional trust. |

Custom nonblank shock types are allowed. The default handler applies a small stability reduction based on severity.

## Validation Rules

Scenario validation currently enforces:

- `name` is required and cannot be blank.
- `ticks` must be greater than `0`.
- `initialPopulation` cannot be negative.
- `food`, `medicine`, `housing`, `adminCapacity`, and `productionCapacity` cannot be negative.
- Each shock `tick` must be greater than or equal to `0` and less than `ticks`.
- Each shock `type` is required and cannot be blank.
- Each shock `severity` must be between `0` and `1`.

The JSON loader is case-insensitive, but new scenarios should use the camelCase field names shown here.

## Authoring A New Scenario

1. Copy one of the files in [../../examples](../../examples).
2. Give the scenario a clear `name` and choose a deterministic `seed`.
3. Set `ticks` long enough for both the shock and recovery pattern you want to observe.
4. Choose `initialPopulation` and `initialResources` so scarcity is visible but not accidental.
5. Add one or more shocks with ticks inside the run window.
6. Run the scenario with the CLI:

```powershell
dotnet run --project src/PolityKit.Sim.Cli -- run `
  --scenario examples/village-food-crisis.json `
  --out runs/village-food-crisis
```

After the run, inspect `summary.json`, `metrics.csv`, and `events.jsonl` in the output folder.
