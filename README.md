# PolityKit

**PolityKit** is an open-source simulation framework for exploring how governance, allocation, and institutional systems behave under stress, scarcity, and changing assumptions.

PolityKit does **not** attempt to prove which political, economic, or social system is best. It is an assumption laboratory:

> Given these assumptions, how does this system behave when conditions change?

The current implementation is a small deterministic simulation stack for comparing starter allocation models against shared scenarios, shocks, and metrics.

## Current Status

PolityKit includes:

- A .NET 10 solution with separate projects for core domain types, engine logic, allocation models, metrics, scenarios, CLI, and API.
- A deterministic simulation engine with seeded world generation, scheduled shocks, world-rule application, metric recording, and per-model run results.
- Core world/domain types for citizens, resources, institutions, environment, scenarios, shocks, system decisions, events, metrics, and seeded random sources.
- Three starter allocation models:
  - `NeedBasedAllocation`
  - `MarketBasedAllocation`
  - `HierarchyBasedAllocation`
- Five starter metrics:
  - `Needs Met`
  - `Inequality`
  - `Trust`
  - `Severe Failures`
  - `Administrative Load`
- Built-in and JSON-loaded scenarios, including the built-in `Village Food Crisis`.
- Example scenario files under `examples/`.
- A CLI runner that writes run bundles to disk.
- An ASP.NET Core API for listing models, metrics, scenarios, and creating/querying simulation runs.
- Automated unit and integration tests for the simulation libraries and API.

Still early / not built yet:

- A richer CLI command surface beyond running simulations and listing models.
- Persistent run storage beyond the in-memory API store.
- Frontend dashboards or visualization tools.
- A formal scenario JSON schema file.
- Real-world calibration or policy-grade validation. The models remain intentionally simplified.

## Repository Layout

```text
PolityKit/
  PolityKit.slnx
  README.md
  LICENSE
  docs/
    ROADMAP.md
  examples/
    README.md
    corruption-stress.json
    housing-displacement.json
    medicine-shortage.json
    village-food-crisis.json
  src/
    PolityKit.Sim.Api/
    PolityKit.Sim.Cli/
    PolityKit.Sim.Core/
    PolityKit.Sim.Engine/
    PolityKit.Sim.Metrics/
    PolityKit.Sim.Models/
    PolityKit.Sim.Scenarios/
  tests/
    PolityKit.Sim.Api.Tests/
    PolityKit.Sim.Tests/
```

### Project Roles

- `PolityKit.Sim.Core`: shared domain types and contracts, including world state, citizens, resources, decisions, events, metrics, scenarios, and random sources.
- `PolityKit.Sim.Engine`: deterministic simulation orchestration, world creation, tick execution, shock handling, world-rule application, and run results.
- `PolityKit.Sim.Models`: starter allocation model implementations and model catalog.
- `PolityKit.Sim.Metrics`: metric calculators and metric catalog.
- `PolityKit.Sim.Scenarios`: built-in scenarios, scenario validation, JSON loading, name resolution, and cloning helpers.
- `PolityKit.Sim.Cli`: command-line runner for local simulations and run-output files.
- `PolityKit.Sim.Api`: ASP.NET Core HTTP API for simulation metadata and in-memory run management.
- `PolityKit.Sim.Tests`: unit tests for core, engine, metrics, models, scenarios, and example files.
- `PolityKit.Sim.Api.Tests`: API integration tests plus run service, mapper, store, and test-host tests.

## Development

Prerequisite:

- .NET 10 SDK.

Build the solution:

```bash
dotnet build PolityKit.slnx
```

Run the tests:

```bash
dotnet test PolityKit.slnx
```

Run the API:

```bash
dotnet run --project src/PolityKit.Sim.Api
```

The API launch profile uses:

```text
http://localhost:5020
https://localhost:7058
```

Run the CLI help:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- --help
```

List available models:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- list-models
```

Run the default built-in scenario:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run
```

Run an example scenario with selected models:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run \
  --scenario examples/village-food-crisis.json \
  --models need-based-allocation,market-based-allocation,hierarchy-based-allocation \
  --seed 12345 \
  --ticks 120 \
  --out runs/village-food-crisis-12345
```

On Windows PowerShell:

```powershell
dotnet run --project src/PolityKit.Sim.Cli -- run `
  --scenario examples/village-food-crisis.json `
  --models need-based-allocation,market-based-allocation,hierarchy-based-allocation `
  --seed 12345 `
  --ticks 120 `
  --out runs/village-food-crisis-12345
```

CLI run output:

```text
runs/village-food-crisis-12345/
  config.json
  metrics.csv
  events.jsonl
  citizens-final.csv
  summary.json
```

## How To Use

You can use PolityKit either from the CLI for local run bundles or from the API for programmatic experiments.

### Use the CLI

List the available allocation models:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- list-models
```

Run the built-in `Village Food Crisis` scenario with all models:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run
```

Run a specific example scenario:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run --scenario examples/medicine-shortage.json
```

Compare two models with a fixed seed and shorter duration:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run \
  --scenario examples/village-food-crisis.json \
  --models need-based-allocation,market-based-allocation \
  --seed 20260614 \
  --ticks 60 \
  --out runs/food-crisis-compare
```

PowerShell version:

```powershell
dotnet run --project src/PolityKit.Sim.Cli -- run `
  --scenario examples/village-food-crisis.json `
  --models need-based-allocation,market-based-allocation `
  --seed 20260614 `
  --ticks 60 `
  --out runs/food-crisis-compare
```

Change model assumptions with `--param`:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run \
  --scenario examples/corruption-stress.json \
  --models need-based-allocation \
  --param needPriorityWeight=1.25 \
  --param vulnerabilityPriorityWeight=0.75 \
  --out runs/corruption-need-weighted
```

After a run, inspect:

- `summary.json` for final per-model metric summaries.
- `metrics.csv` for metric values by tick.
- `events.jsonl` for the event stream.
- `citizens-final.csv` for final citizen state.
- `config.json` for the scenario, seed, models, metrics, and parameters used.

### Use the API

Start the API:

```bash
dotnet run --project src/PolityKit.Sim.Api
```

The default HTTP endpoint is:

```text
http://localhost:5020
```

List models:

```powershell
Invoke-RestMethod http://localhost:5020/api/models
```

List metrics:

```powershell
Invoke-RestMethod http://localhost:5020/api/metrics
```

List scenarios:

```powershell
Invoke-RestMethod http://localhost:5020/api/scenarios
```

Create a run:

```powershell
$body = @{
  scenario = "village-food-crisis"
  seed = 12345
  ticks = 60
  models = @(
    "need-based-allocation",
    "market-based-allocation"
  )
  parameters = @{
    needPriorityWeight = 1.0
    vulnerabilityPriorityWeight = 0.5
  }
} | ConvertTo-Json -Depth 5

$run = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5020/api/runs `
  -ContentType "application/json" `
  -Body $body

$run
```

Fetch the run detail, metrics, and events:

```powershell
Invoke-RestMethod "http://localhost:5020/api/runs/$($run.id)"
Invoke-RestMethod "http://localhost:5020/api/runs/$($run.id)/metrics"
Invoke-RestMethod "http://localhost:5020/api/runs/$($run.id)/events"
```

List all in-memory runs:

```powershell
Invoke-RestMethod http://localhost:5020/api/runs
```

The API run store is currently in memory. Restarting the API clears the run list.

## API Surface

The current API exposes:

```text
GET  /api/models
GET  /api/metrics
GET  /api/scenarios
GET  /api/runs
POST /api/runs
GET  /api/runs/{id}
GET  /api/runs/{id}/metrics
GET  /api/runs/{id}/events
```

Example run request:

```json
{
  "scenario": "village-food-crisis",
  "seed": 12345,
  "ticks": 120,
  "models": [
    "need-based-allocation",
    "market-based-allocation",
    "hierarchy-based-allocation"
  ],
  "parameters": {
    "needPriorityWeight": 1.0,
    "vulnerabilityPriorityWeight": 0.5
  }
}
```

The API currently stores runs in memory. Restarting the API clears stored runs.

## Example Scenarios

Example JSON scenarios live in `examples/`:

- `village-food-crisis.json`
- `medicine-shortage.json`
- `housing-displacement.json`
- `corruption-stress.json`

Each scenario includes:

- `name`
- `seed`
- `ticks`
- `initialPopulation`
- `initialResources`
- `shocks`

Currently handled shock types include:

- `CropFailure`
- `MedicineShortage`
- `HousingLoss`
- `AdministrativeOverload`
- `AdminLoss`
- `CorruptionSpike`

## Core Concepts

### World State

`WorldState` represents the current simulation state: tick, population, resources, institutional state, environment, and recorded events.

### Citizens

Citizens are simulated agents with simple properties such as food need, health need, housing need, wealth, social power, trust in system, and vulnerability.

### Resources

The current resource pool includes:

- Food
- Medicine
- Housing
- Administrative capacity
- Production capacity

### System Models

A system model decides how a simulated society responds to current world conditions. Starter models use neutral mechanical names:

- `NeedBasedAllocation`: prioritizes unmet need and vulnerability.
- `MarketBasedAllocation`: prioritizes wealth and demand pressure.
- `HierarchyBasedAllocation`: prioritizes social power, obligation, and visible need.

Each model exposes a manifest with assumptions and known failure modes.

### Engine

For each selected model, the engine:

1. Creates a fresh seeded world from the scenario.
2. Applies scheduled shocks on each tick.
3. Asks the model for a `SystemDecision`.
4. Applies shared world rules.
5. Records configured metrics.
6. Returns per-model worlds, events, and metric series.

### Metrics

Metrics describe what happened during a run:

- `Needs Met`: share of citizens with food, health, and housing needs met.
- `Inequality`: Gini-style wealth inequality score.
- `Trust`: average of institutional trust and citizen trust.
- `Severe Failures`: severe events plus citizens in severe need or very low trust.
- `Administrative Load`: appeal backlog plus administrative overflow events.

## Guiding Principles

### Make Assumptions Visible

Models should expose the assumptions that drive their behavior. Important values should be documented and configurable where practical.

### Prefer Comparison Over Conclusion

PolityKit should compare systems under shared conditions, not declare universal winners.

Useful questions include:

- Which model fails first under scarcity?
- Which model preserves trust longest?
- Which model is most sensitive to corruption?
- Which model recovers fastest after a shock?
- Which model produces high output but high inequality?
- Which model degrades gracefully as resources decline?

### Separate World Rules From System Decisions

The engine defines shared world mechanics. System models decide what actions to take within that world.

For example:

- The engine defines what happens when a citizen does not receive enough food.
- A system model decides who receives food.

This separation keeps comparisons consistent.

### Reproducibility Matters

Given the same scenario, seed, model, configuration, and version, a run should produce the same result.

Every run records:

- Scenario name.
- Model name and version.
- Configuration values.
- Random seed.
- Simulation duration.
- Metrics.
- Event log.
- Final citizen state.

## Interpreting Results

PolityKit results should be read carefully.

A result does not mean:

```text
This system works in the real world.
```

It means:

```text
Under this scenario, using this model, with these assumptions, this behavior emerged.
```

The most interesting results are often patterns: sudden collapse after a threshold, slow decline hidden by short-term stability, fairness paired with administrative load, or fast crisis response paired with poor long-term trust.

## Contributing

Useful contribution areas include:

- New scenario files and stress cases.
- New metrics.
- New model variants with explicit assumptions.
- CLI improvements.
- API persistence and query features.
- Visualization and dashboard tooling.
- Documentation for assumptions, formulas, and model limitations.
- Tests for new behavior.

The project should welcome disagreement by turning it into inspectable model-building: fork the model, change the assumptions, run the same scenario, and compare the results.

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md).

## License

PolityKit is licensed under the Apache License 2.0. See [LICENSE](LICENSE).

## Disclaimer

PolityKit is a simulation framework for exploring assumptions and system behavior. It should not be used as a source of political, economic, legal, or policy authority. Models are simplified by design and should be interpreted as experimental tools rather than representations of real societies.
