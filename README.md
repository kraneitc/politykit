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
- A golden interpreted run bundle under `examples/golden-interpreted-run`.
- A CLI runner that writes run bundles to disk.
- An ASP.NET Core API for listing models, metrics, scenarios, creating/querying persisted simulation runs, and retrieving dashboard-ready run data.
- Documented AI boundaries that keep AI analysis optional, advisory, and separate from authoritative simulation data.
- Automated unit and integration tests for the simulation libraries and API.

Still early / not built yet:

- A richer CLI command surface beyond running simulations and listing models.
- Frontend dashboards or visualization tools.
- AI provider integrations or generated advisory summaries.
- Real-world calibration or policy-grade validation. The models remain intentionally simplified.

## Repository Layout

```text
PolityKit/
  PolityKit.slnx
  README.md
  LICENSE
  docs/
    politykit/
      add-metric.md
      add-model.md
      add-scenario.md
      contributing.md
      governance-presets.md
      scenarios.md
      scenario.schema.json
      implementation/
        politykit-roadmap.md
        v0.6-implementation-plan.md
        v0.7-implementation-plan.md
        v0.8-implementation-plan.md
    charterfall/
      implementation/
  examples/
    README.md
    corruption-stress.json
    golden-interpreted-run/
    housing-displacement.json
    interpretability-demo.json
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
- `PolityKit.Sim.Api`: ASP.NET Core HTTP API for simulation metadata, file-backed run storage, and dashboard-ready run inspection.
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
  ai-analysis.json
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

Compare a governance preset against the three mechanical baseline models:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run \
  --scenario examples/village-food-crisis.json \
  --models need-based-allocation,market-based-allocation,hierarchy-based-allocation,participatory-commons \
  --seed 20260614 \
  --ticks 60 \
  --out runs/food-crisis-baseline-preset-compare
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

Run a local parameter sweep and write report files:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- sweep \
  --scenario examples/village-food-crisis.json \
  --models need-based-allocation \
  --seed 12345 \
  --ticks 60 \
  --sweep needPriorityWeight=0.75,1.0,1.25 \
  --sweep vulnerabilityPriorityWeight=0.25,0.5 \
  --out runs/food-crisis-need-sweep
```

PowerShell version:

```powershell
dotnet run --project src/PolityKit.Sim.Cli -- sweep `
  --scenario examples/village-food-crisis.json `
  --models need-based-allocation `
  --seed 12345 `
  --ticks 60 `
  --sweep needPriorityWeight=0.75,1.0,1.25 `
  --sweep vulnerabilityPriorityWeight=0.25,0.5 `
  --out runs/food-crisis-need-sweep
```

After a run, inspect:

- `summary.json` for final per-model metrics, event counts, and notable metric changes with breadcrumb text and nearby events.
- `ai-analysis.json` for whether advisory AI analysis was used. Default simulation runs record `used: false`.
- `ai-summary.json` after running the optional `summary` command for an advisory generated interpretation of the run.
- `ai-model-critique.json` after running the optional `critique-model` command for advisory model critique fields.
- `ai-anomalies.json` after running the optional `ai-anomalies` command against a stress summary.
- `metrics.csv` for metric values by tick.
- `events.jsonl` for the event stream, including model, resource, count, backlog, severity, and trust-delta context where available.
- `citizens-final.csv` for final citizen state.
- `config.json` for the scenario, seed, models, metrics, and parameters used.

Generate an advisory AI summary artifact from a completed local run bundle using the built-in fake provider:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- summary \
  --bundle runs/village-food-crisis-12345 \
  --provider fake
```

The fake provider is local and deterministic. It creates `ai-summary.json` with provenance and boundary metadata, but the generated text is advisory interpretation, not simulation data.

Generate a validated scenario suggestion draft from the same completed bundle:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- suggest-scenario \
  --bundle runs/village-food-crisis-12345 \
  --provider fake
```

This writes `scenario-suggestion-draft.json` only when the provider returned a draft and the existing scenario validator accepts it. The draft remains a proposed artifact for review, not an automatically accepted scenario file.

Generate an advisory model critique from the same completed bundle:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- critique-model \
  --bundle runs/village-food-crisis-12345 \
  --model regulated-market \
  --provider fake
```

This writes `ai-model-critique.json` with model manifest assumptions, governance dimensions, run metrics, failure diagnostics, and provenance. Critiques are prompts for human review; they are not proof that a model is correct or incorrect, and they do not rewrite model code.

After a sweep, inspect:

- `sweep-summary.json` for every parameter combination, final metrics, best/worst runs by metric, and a ranked sensitivity report.
- `sweep-metrics.csv` for a flat table of final metrics by sweep run.
- `run-001/`, `run-002/`, and so on for full per-combination run bundles.

Run a local stress sweep across scenarios, seeds, models, and parameter values:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- stress \
  --scenario village-food-crisis \
  --scenario examples/medicine-shortage.json \
  --seed 111,222 \
  --models need-based-allocation,market-based-allocation \
  --sweep needPriorityWeight=0.75,1.0,1.25 \
  --out runs/v0-6-stress
```

Run a stress sweep that compares baseline models with multiple governance presets across existing challenge scenarios:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- stress \
  --grid-name baseline-preset-comparison \
  --scenario examples/medicine-shortage.json \
  --scenario examples/corruption-stress.json \
  --seed 111,222 \
  --models need-based-allocation,market-based-allocation,hierarchy-based-allocation,participatory-commons,regulated-market,technocratic-administration \
  --sweep needWeightMultiplier=0.8,1.0,1.2 \
  --out runs/baseline-preset-comparison-stress
```

After a stress sweep, inspect:

- `stress-summary.json` for collapse events, sensitivity, and model robustness summaries.
- `stress-metrics.csv` for final metrics by stress run.
- `modelRobustness` to compare collapse rate, recovery rate, collapse timing, most sensitive parameter, and best/worst scenario names. Treat these as simulation diagnostics for the tested assumptions, not as real-world model rankings.

For mixed baseline-plus-preset stress runs, `modelRobustness` uses the same summary shape for both families:

```json
{
  "modelRobustness": [
    {
      "model": "NeedBasedAllocation",
      "runsCompleted": 12,
      "mostSensitiveParameter": "needWeightMultiplier"
    },
    {
      "model": "CompositeGovernance:participatory-commons",
      "runsCompleted": 12,
      "mostSensitiveParameter": "needWeightMultiplier"
    }
  ]
}
```

Generate an advisory batch anomaly artifact from a completed stress summary:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- ai-anomalies \
  --stress-summary runs/baseline-preset-comparison-stress/stress-summary.json \
  --provider fake
```

This writes `ai-anomalies.json` with structured anomaly candidates for model, scenario, seed, metric, observed value, and explanation. The artifact flags provider output that references run IDs, models, scenarios, seeds, or metrics that were not present in the source stress summary.

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

Fetch the run detail, metrics, events, and dashboard-ready payload:

```powershell
Invoke-RestMethod "http://localhost:5020/api/runs/$($run.id)"
Invoke-RestMethod "http://localhost:5020/api/runs/$($run.id)/metrics"
Invoke-RestMethod "http://localhost:5020/api/runs/$($run.id)/events"
Invoke-RestMethod "http://localhost:5020/api/runs/$($run.id)/dashboard"
```

Rerun a completed run with the same scenario and seed:

```powershell
$rerun = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5020/api/runs/$($run.id)/rerun" `
  -ContentType "application/json" `
  -Body (@{} | ConvertTo-Json)
```

Rerun with changed model parameters while keeping the original seed:

```powershell
$rerun = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5020/api/runs/$($run.id)/rerun" `
  -ContentType "application/json" `
  -Body (@{
    parameters = @{
      needPriorityWeight = 1.25
    }
  } | ConvertTo-Json -Depth 5)
```

Compare two completed runs:

```powershell
Invoke-RestMethod "http://localhost:5020/api/runs/$($run.id)/compare/$($rerun.id)"
```

Run a basic parameter sweep:

```powershell
$sweep = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5020/api/runs/sweep" `
  -ContentType "application/json" `
  -Body (@{
    scenario = "village-food-crisis"
    seed = 12345
    ticks = 60
    models = @("need-based-allocation")
    parameters = @{
      fixedWeight = 1.0
    }
    sweep = @{
      needPriorityWeight = @(0.75, 1.0, 1.25)
      vulnerabilityPriorityWeight = @(0.25, 0.5)
    }
  } | ConvertTo-Json -Depth 6)
```

The sweep response includes each generated run, its parameter combination, final metrics, best/worst metric runs, and sensitivity rankings.

Run a stress sweep from the API:

```powershell
$stress = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5020/api/runs/stress" `
  -ContentType "application/json" `
  -Body (@{
    gridName = "v0-6-stress"
    scenarios = @("village-food-crisis", "examples/medicine-shortage.json")
    seeds = @(111, 222)
    models = @("need-based-allocation", "market-based-allocation")
    sweep = @{
      needPriorityWeight = @(0.75, 1.0, 1.25)
    }
  } | ConvertTo-Json -Depth 6)
```

The stress response includes run summaries, collapse events, sensitivity rankings, and `modelRobustness` summaries for comparing models under the tested assumptions.

API stress requests can mix baseline names and preset IDs:

```powershell
$stress = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5020/api/runs/stress" `
  -ContentType "application/json" `
  -Body (@{
    gridName = "baseline-preset-comparison"
    scenarios = @("village-food-crisis")
    seeds = @(111, 222)
    ticks = 60
    models = @(
      "need-based-allocation",
      "market-based-allocation",
      "hierarchy-based-allocation",
      "participatory-commons",
      "regulated-market"
    )
    sweep = @{
      needWeightMultiplier = @(0.8, 1.0, 1.2)
    }
  } | ConvertTo-Json -Depth 6)
```

List all persisted API runs:

```powershell
Invoke-RestMethod http://localhost:5020/api/runs
```

The API stores run records as JSON files under `data/runs` by default. Configure `RunStorage:Directory` to change the storage location.

API run, sweep, stress, and comparison responses include `aiAnalysis`. By default this records `used: false`; AI-generated text, if added later, must stay advisory and record provenance such as source run IDs, files, scenarios, models, seeds, metrics, provider, model, prompt template version, and creation time rather than becoming authoritative simulation data.

The shared analysis layer includes an optional provider abstraction with local disabled mode. By default AI analysis returns `AI analysis is not configured.` without requiring any provider package or sending run data externally.

For local examples and tests, configure `AiAnalysis:Enabled=true` and `AiAnalysis:ProviderName=fake`, then call `POST /api/runs/{id}/ai/summary` to generate an advisory run-summary artifact for a stored run, `POST /api/runs/{id}/ai/scenario-suggestions` to generate a validated scenario suggestion draft, `POST /api/runs/{id}/ai/model-critique?model=regulated-market` to generate an advisory model critique, or `POST /api/runs/stress/ai/anomalies` with a stress request body to generate advisory anomaly candidates from the resulting stress summary. The older `POST /api/runs/{id}/ai-summary` and `POST /api/runs/{id}/scenario-suggestions` aliases remain available for compatibility.

See [AI boundaries and safety](docs/politykit/ai-boundaries.md) for the optional-AI rule, advisory-output rule, provenance shape, provider guardrails, and privacy note for data sent to external providers.

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
GET  /api/runs/{id}/dashboard
POST /api/runs/{id}/ai/summary
POST /api/runs/{id}/ai/scenario-suggestions
POST /api/runs/{id}/ai/model-critique
POST /api/runs/{id}/rerun
GET  /api/runs/{id}/compare/{comparisonId}
POST /api/runs/sweep
POST /api/runs/stress
POST /api/runs/stress/ai/anomalies
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

The API stores runs in the configured file-backed run store.

## Example Scenarios

Example JSON scenarios live in `examples/`:

- `village-food-crisis.json`
- `medicine-shortage.json`
- `housing-displacement.json`
- `interpretability-demo.json`
- `corruption-stress.json`

The scenario guide lives at `docs/politykit/scenarios.md`, and the formal schema lives at `docs/politykit/scenario.schema.json`.
The golden interpreted run bundle lives at `examples/golden-interpreted-run`.

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

Composite governance presets appear as `CompositeGovernance:<preset-id>` models. They are simplified bundles of governance dimensions, not claims about real societies. Their manifests include both preset-level assumptions and per-dimension assumptions such as allocation mechanism, decision authority, accountability, information flow, property regime, and appeal process.

See [Governance presets](docs/politykit/governance-presets.md) for the current preset list, manifest interpretation rules, and wording boundaries for comparing presets against baseline models.

### Governance Presets

Governance presets let users compare bundles of declared assumptions without turning a single label into a whole model. Current preset IDs include:

- `participatory-commons`
- `regulated-market`
- `central-planning`
- `patronage-hierarchy`
- `mutual-aid-federation`
- `technocratic-administration`

Treat these IDs as experiment names. A preset result means "under this scenario, seed, model version, and declared assumptions, this behavior emerged." It does not mean the preset label describes or evaluates a real-world society.

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

### Keep AI Advisory

AI analysis is optional and never required to run simulations. AI output may help explain or propose follow-up artifacts, but it is not simulation data and must not change deterministic run results. Any AI-assisted artifact should record whether AI was used, the input run IDs or files it read, the scenarios, models, seeds, and metrics it referenced, and the provider/model that generated the text.

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

Governance preset results need the same caution. A preset label is shorthand for a manifest, not an argument about the real world. Compare the selected dimensions, assumptions, failure modes, scenario, seed, and parameter grid before drawing conclusions.

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

Contributor docs:

- [Contributing guide](docs/politykit/contributing.md)
- [How to add a model](docs/politykit/add-model.md)
- [Governance presets](docs/politykit/governance-presets.md)
- [How to add a metric](docs/politykit/add-metric.md)
- [How to add a scenario](docs/politykit/add-scenario.md)
- [Scenario format reference](docs/politykit/scenarios.md)

## Roadmap

See [docs/politykit/implementation/politykit-roadmap.md](docs/politykit/implementation/politykit-roadmap.md).

## License

PolityKit is licensed under the Apache License 2.0. See [LICENSE](LICENSE).

## Disclaimer

PolityKit is a simulation framework for exploring assumptions and system behavior. It should not be used as a source of political, economic, legal, or policy authority. Models are simplified by design and should be interpreted as experimental tools rather than representations of real societies.
