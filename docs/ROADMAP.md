# PolityKit Roadmap

PolityKit is a governance and allocation simulation framework. The repository is now past the initial scaffold: it includes a deterministic simulation engine, starter allocation models, metrics, scenarios, CLI output, an API surface, and automated tests.

This roadmap tracks what exists now, what remains for the active interpretability milestone, and the planned direction toward a public framework release.

## As-Built Baseline

Current repository structure:

```text
PolityKit/
  PolityKit.slnx
  README.md
  LICENSE
  docs/
    add-metric.md
    add-model.md
    add-scenario.md
    contributing.md
    ROADMAP.md
    scenarios.md
    scenario.schema.json
  examples/
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

Current implementation state:

- `PolityKit.Sim.Core` contains world state, citizens, resources, events, metrics, scenarios, system decisions, model manifests, and seeded random contracts.
- `PolityKit.Sim.Engine` contains deterministic world creation, shock handling, tick execution, world-rule application, and per-model run results.
- `PolityKit.Sim.Models` contains three starter allocation models: `NeedBasedAllocation`, `MarketBasedAllocation`, and `HierarchyBasedAllocation`.
- `PolityKit.Sim.Metrics` contains five starter metrics: needs met, inequality, trust, severe failures, and administrative load.
- `PolityKit.Sim.Scenarios` contains built-in scenarios, JSON loading, scenario name resolution, cloning helpers, and validation.
- `PolityKit.Sim.Cli` can list models and run simulations that emit `config.json`, `metrics.csv`, `events.jsonl`, `citizens-final.csv`, and `summary.json`.
- `PolityKit.Sim.Api` exposes models, metrics, scenarios, and in-memory simulation runs through HTTP endpoints.
- `examples/` contains JSON scenario files for food, medicine, housing, and corruption stress cases.
- `tests/` contains unit and API integration coverage for the current stack.
- The solution targets .NET 10.
- The project is licensed under Apache 2.0.

## Project Vision

PolityKit should become an open-source framework for exploring how different governance, allocation, and institutional systems behave under stress, uncertainty, scarcity, and changing assumptions.

It should not try to prove that one political or economic system is superior. Instead, it should make assumptions explicit, run comparable scenarios, and help users examine trade-offs, failure modes, graceful degradation, and resilience.

A useful framing:

> This project does not show how society works. It shows what follows from a defined set of assumptions.

## Architectural Direction

The current implementation preserves these main boundaries:

| Project | Current Responsibility |
|---|---|
| `PolityKit.Sim.Core` | Domain primitives, shared contracts, world state, citizens, resources, decisions, events, and configuration types. |
| `PolityKit.Sim.Engine` | Deterministic simulation loop, tick orchestration, world-rule application, shock handling, and run lifecycle. |
| `PolityKit.Sim.Models` | Allocation and governance model implementations. |
| `PolityKit.Sim.Metrics` | Metric contracts, calculators, summaries, and export-friendly metric records. |
| `PolityKit.Sim.Scenarios` | Scenario definitions, loading, validation, and bundled starter scenarios. |
| `PolityKit.Sim.Cli` | Local simulation runner and file-output workflow. |
| `PolityKit.Sim.Api` | HTTP API for dashboards, tools, and future integrations. |

## Guiding Principles

### Keep the Engine Neutral

The simulation engine should not contain system-specific ideology or allocation preference. It should load scenarios, run ticks, apply decisions through common rules, collect metrics, and export results.

System-specific behavior belongs in model implementations.

### Separate World Rules From Model Decisions

World rules describe what happens when needs are or are not met.

Examples:

- If citizens do not receive enough food, health declines.
- If institutional corruption rises, trust declines.
- If administrative load exceeds capacity, backlog grows.

Model decisions describe how a system allocates resources or responds to events.

Examples:

- A market model may allocate based on wealth and price.
- A need-based model may allocate based on vulnerability and urgency.
- A hierarchy model may allocate based on rank, obligation, and patronage.

### Make Assumptions Explicit

Avoid burying assumptions in code. Important constants and weights should be configurable and documented in model manifests where practical.

Instead of:

```csharp
innovation += 5;
```

Prefer:

```csharp
innovation += competition * config.CompetitionInnovationMultiplier;
```

### Design for Reproducibility

Given the same scenario, model, configuration, and random seed, a simulation should produce the same result.

Every run should record:

- Scenario name.
- Model name and version.
- Configuration values.
- Random seed.
- Simulation duration.
- Metrics.
- Event log.

### Focus on Failure Modes

The most useful question is not "Which system is best?"

Better questions:

- What breaks first?
- Under what conditions does this model degrade?
- Does it fail suddenly or gradually?
- Does it recover?
- Which groups are affected first?
- Which assumptions drive the result?

## Current Core Types

The first implementation uses simple, inspectable types. The examples below show the shape of the core contracts; the source files are the authoritative definitions.

### World State

```csharp
public sealed class WorldState
{
    public int Tick { get; set; }
    public Population Population { get; init; } = new();
    public ResourcePool Resources { get; init; } = new();
    public InstitutionalState Institutions { get; init; } = new();
    public EnvironmentState Environment { get; init; } = new();
}
```

### Citizen

```csharp
public sealed class Citizen
{
    public Guid Id { get; init; }
    public int FoodNeed { get; set; }
    public int HealthNeed { get; set; }
    public int HousingNeed { get; set; }
    public int Wealth { get; set; }
    public int SocialPower { get; set; }
    public int TrustInSystem { get; set; }
    public int Vulnerability { get; set; }
}
```

### System Model

```csharp
public interface ISystemModel
{
    string Name { get; }

    SystemDecision Decide(WorldState world, SystemContext context);
}
```

### System Decision

```csharp
public sealed class SystemDecision
{
    public List<ResourceAllocation> Allocations { get; init; } = [];
    public List<PolicyChange> PolicyChanges { get; init; } = [];
    public List<InstitutionalAction> InstitutionalActions { get; init; } = [];
}
```

### Metric

```csharp
public interface IMetric
{
    string Name { get; }

    double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events);
}
```

### Event

```csharp
public sealed class SimulationEvent
{
    public int Tick { get; init; }
    public string Type { get; init; } = "";
    public string Description { get; init; } = "";
    public Dictionary<string, object> Data { get; init; } = [];
}
```

## Scenario Format

Scenarios should define starting conditions and scheduled shocks.

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
    "adminCapacity": 80
  },
  "shocks": [
    {
      "tick": 20,
      "type": "CropFailure",
      "severity": 0.4
    },
    {
      "tick": 45,
      "type": "AdministrativeOverload",
      "severity": 0.3
    }
  ]
}
```

## Model Manifest Format

Each model should include a manifest that explains its purpose and assumptions.

```json
{
  "model": "HierarchyBasedAllocation",
  "description": "Allocates resources according to social rank, obligation, and patronage.",
  "assumptions": [
    {
      "name": "rankPriorityWeight",
      "default": 0.7,
      "description": "How strongly social rank affects resource priority."
    },
    {
      "name": "eliteObligationWeight",
      "default": 0.4,
      "description": "How strongly high-rank citizens are expected to protect dependents."
    }
  ],
  "knownFailureModes": [
    "low social mobility",
    "elite capture",
    "under-provision to low-rank citizens",
    "dependence on competent elites"
  ]
}
```

## Output Format

Early versions should use simple files rather than a database.

Suggested output folder:

```text
runs/2026-06-14-village-food-crisis/
  config.json
  metrics.csv
  events.jsonl
  citizens-final.csv
  summary.json
```

Files are easy to inspect, compare, commit as examples, and consume from later dashboards.

## Roadmap By Version

### Version 0.0 - Scaffold

Status: complete.

Scope:

- Create solution file.
- Create source projects.
- Create `docs/`, `examples/`, and `tests/` folders.
- Establish README and roadmap.
- Choose license.

Completed cleanup:

- Placeholder classes have been replaced with named domain files.
- The default weather forecast API sample has been replaced with PolityKit endpoints.

### Version 0.1 - Smallest Useful Simulation

Status: complete.

Goal: prove the basic concept from the CLI.

Features:

- Core domain types in `PolityKit.Sim.Core`.
- Deterministic engine loop in `PolityKit.Sim.Engine`.
- One world model.
- Three resources: food, medicine, housing.
- Three mechanical allocation models:
  - `NeedBasedAllocation`
  - `MarketBasedAllocation`
  - `HierarchyBasedAllocation`
- Five metrics:
  - Needs met.
  - Inequality.
  - Trust.
  - Severe failures.
  - Administrative load.
- One bundled scenario:
  - Village food crisis.
- CLI runner.
- CSV and JSONL output.
- Unit tests for deterministic behavior and core metric calculations.

Success criteria:

- Same scenario plus same seed produces the same result.
- Different models produce visibly different outcomes.
- A non-author can inspect the output files and understand the result.

### Version 0.2 - Better Interpretability

Status: active.

Goal: make results explainable.

Already built:

- Model manifests.
- Scenario validation.
- Additional scenarios:
  - Medicine shortage.
  - Housing displacement.
  - Corruption stress.
- Model parameters exposed through CLI/API run requests.
- Basic run summaries with final per-model metric values.
- Event log output through CLI `events.jsonl` and API run event endpoints.
- Formal scenario JSON schema.
- Scenario authoring documentation for fields, validation rules, and supported shock types.
- Summary output that links notable metric changes to nearby shocks and events with simple causal breadcrumbs.
- Tests for summary interpretation.
- Richer event log context for shocks, allocations, administrative pressure, trust shifts, and severe failures.
- Tests for richer event fields.
- Tests for schema validity, example scenario compatibility, summary generation, and event-link breadcrumbs.
- Golden interpreted run bundle under `examples/golden-interpreted-run`.

Remaining scope:

- More detailed trust and backlog behavior.

Success criteria:

- A user can inspect a metric change and identify likely causes.
- Model assumptions are visible without reading code.
- Scenario files can be authored against a documented schema.

### Version 0.3 - Contribution-Friendly Models

Status: started.

Goal: make it easy for contributors to add models.

Already built:

- Stable model interface.
- Documentation for adding models.
- Documentation for adding metrics.
- Documentation for adding scenarios.
- Contributor guide.

Remaining scope:

- Example custom model.
- Validation for model manifests.
- Tests that demonstrate model contract expectations.

Success criteria:

- A contributor can add a simple model without understanding the entire engine.

### Version 0.4 - API And Dashboard Foundation

Status: started.

Goal: expose completed runs for tools and dashboards.

Already built:

- Replace default API sample with PolityKit endpoints.
- API endpoint to list available runs.
- API endpoint to retrieve run summaries.
- API endpoint to retrieve metrics and events.

Remaining scope:

- File-backed or otherwise persistent run storage.
- API support for inspecting completed CLI run bundles.
- Basic dashboard or dashboard-ready JSON output.

Success criteria:

- A local dashboard or external tool can inspect completed run data without parsing internal files directly.

### Version 0.5 - Parameter Exploration

Goal: support interactive exploration.

Features:

- Configurable model parameters.
- Fast reruns from the same seed.
- Before-and-after comparison.
- Exportable run bundles.
- Basic parameter sweep support.

Success criteria:

- Users can change assumptions and compare how outcomes change.

### Version 0.6 - Stress Testing And Sensitivity

Goal: find thresholds and fragile regions.

Features:

- Batch parameter sweeps.
- Sensitivity reports.
- Collapse-threshold detection.
- Scenario challenge packs.
- Model robustness summaries.

Success criteria:

- The system can report which parameters most strongly affect failure or recovery.

### Version 0.7 - Composite Governance Presets

Goal: move from simple allocation models to richer governance bundles.

Features:

- Composable governance dimensions:
  - Allocation mechanism.
  - Decision authority.
  - Accountability mechanism.
  - Information flow.
  - Property regime.
  - Appeal process.
- Presets for common system types.
- Comparison against mechanical baseline models.

Success criteria:

- A user can compare regime-like systems without relying on caricatured single-label implementations.

### Version 0.8 - Optional AI-Assisted Exploration

Goal: add optional AI analysis around reproducible simulation outputs.

Features:

- AI-generated run summaries.
- AI-assisted scenario suggestions.
- AI-assisted model critique.
- AI-assisted anomaly detection across run batches.

Success criteria:

- AI helps explain and explore results without invisibly changing core simulation rules.

### Version 1.0 - Public Framework Release

Goal: provide a stable, documented, extensible simulation framework.

Features:

- Stable core engine API.
- Stable model API.
- Stable metric API.
- Reproducible run format.
- Dashboard or API-backed inspection workflow.
- Contributor documentation.
- Example models.
- Example scenarios.
- Known limitations documentation.

Success criteria:

- External contributors can add models, metrics, and scenarios.
- Runs are reproducible and inspectable.
- The project clearly communicates its limitations and purpose.

## Recommended v0.2 Build Sequence

1. Add a formal scenario JSON schema under `docs/`.
2. Add scenario authoring documentation that covers required fields, optional fields, validation rules, and supported shock types.
3. Review event payloads and add useful context fields where interpretability is currently weak.
4. Add an interpretability layer for run summaries that detects notable metric changes.
5. Link notable metric changes to nearby shocks, severe events, administrative pressure, and resource allocation events.
6. Include the interpretability output in CLI `summary.json`.
7. Expose the same interpreted summary shape through the API run detail response if it fits the current contract cleanly.
8. Add tests for schema validity, example scenario compatibility, event payload expectations, and summary interpretation.
9. Update `README.md` so the current status and usage docs describe the completed v0.2 behavior.

## Key Risks

### Hidden Bias

Risk: assumptions are embedded invisibly in code.

Mitigation: expose parameters, require manifests, and document known limitations.

### Ideological Capture

Risk: the project becomes a debate over labels rather than mechanisms.

Mitigation: use mechanical model names initially, allow competing model versions, and focus on failure modes.

### False Authority

Risk: users treat outputs as predictions or proof.

Mitigation: use careful wording, display assumptions alongside results, and keep limitations visible.

### Over-Complexity

Risk: the model grows too large to understand.

Mitigation: start small, keep examples simple, and maintain explainability as a core requirement.

### Poor Reproducibility

Risk: results cannot be repeated.

Mitigation: seed all randomness, store full run configuration, and version models and scenarios.

## Definition Of A Strong First Public Release

A strong first release should include:

- Three simple system models.
- Three simple scenarios.
- Five metrics.
- Reproducible run outputs.
- A basic run-inspection workflow.
- Model manifests.
- Clear README.
- Contribution guide.
- Known limitations page.

The first release should be small, inspectable, and honest.
