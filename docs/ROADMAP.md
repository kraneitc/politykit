# PolityKit Roadmap

PolityKit is a governance and allocation simulation framework. The repository is currently scaffolded as a .NET 10 solution, with project boundaries in place but no implemented simulation behavior yet.

This roadmap is written from the current scaffold forward. It distinguishes what exists now from the planned simulation architecture.

## As-Built Baseline

Current repository structure:

```text
PolityKit/
  PolityKit.slnx
  README.md
  LICENSE
  docs/
    ROADMAP.md
  examples/
  src/
    PolityKit.Sim.Api/
    PolityKit.Sim.Cli/
    PolityKit.Sim.Core/
    PolityKit.Sim.Engine/
    PolityKit.Sim.Metrics/
    PolityKit.Sim.Models/
    PolityKit.Sim.Scenarios/
  tests/
```

Current implementation state:

- `PolityKit.Sim.Api` is an ASP.NET Core scaffold with the default weather forecast endpoint.
- `PolityKit.Sim.Cli` is a console scaffold that prints "Hello, World!".
- `PolityKit.Sim.Core`, `PolityKit.Sim.Engine`, `PolityKit.Sim.Metrics`, `PolityKit.Sim.Models`, and `PolityKit.Sim.Scenarios` are placeholder class libraries.
- `examples/` and `tests/` exist as empty top-level folders.
- The solution targets .NET 10.
- The project is licensed under Apache 2.0.

## Project Vision

PolityKit should become an open-source framework for exploring how different governance, allocation, and institutional systems behave under stress, uncertainty, scarcity, and changing assumptions.

It should not try to prove that one political or economic system is superior. Instead, it should make assumptions explicit, run comparable scenarios, and help users examine trade-offs, failure modes, graceful degradation, and resilience.

A useful framing:

> This project does not show how society works. It shows what follows from a defined set of assumptions.

## Architectural Direction

The scaffold already suggests the main boundaries. The first implementation should preserve these responsibilities:

| Project | Planned Responsibility |
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

## Target Core Types

The first implementation should start with simple, inspectable types.

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

Status: mostly complete.

Scope:

- Create solution file.
- Create source projects.
- Create `docs/`, `examples/`, and `tests/` folders.
- Establish README and roadmap.
- Choose license.

Remaining cleanup:

- Replace default placeholder classes with named domain files as implementation begins.
- Remove default weather forecast API sample when real API endpoints exist.

### Version 0.1 - Smallest Useful Simulation

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

Goal: make results explainable.

Features:

- Richer event log.
- Model manifests.
- Scenario validation.
- More detailed trust and backlog behavior.
- Additional scenarios:
  - Medicine shortage.
  - Administrative overload.
- Summary output that links metric changes to relevant events.

Success criteria:

- A user can inspect a metric change and identify likely causes.
- Model assumptions are visible without reading code.

### Version 0.3 - Contribution-Friendly Models

Goal: make it easy for contributors to add models.

Features:

- Stable model interface.
- Documentation for adding models.
- Documentation for adding metrics.
- Documentation for adding scenarios.
- Example custom model.
- Validation for model manifests.
- Tests that demonstrate model contract expectations.

Success criteria:

- A contributor can add a simple model without understanding the entire engine.

### Version 0.4 - API And Dashboard Foundation

Goal: expose completed runs for tools and dashboards.

Features:

- Replace default API sample with PolityKit endpoints.
- API endpoint to list available runs.
- API endpoint to retrieve run summaries.
- API endpoint to retrieve metrics and events.
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

## Recommended Initial Build Sequence

1. Replace placeholder `Class1.cs` files with domain-oriented files.
2. Add core domain contracts in `PolityKit.Sim.Core`.
3. Add a deterministic random source and simulation context.
4. Implement a minimal engine loop.
5. Add one tiny hard-coded scenario to prove the loop.
6. Move the scenario into JSON loading under `PolityKit.Sim.Scenarios`.
7. Implement `NeedBasedAllocation`.
8. Add metrics and file outputs.
9. Add `MarketBasedAllocation` and `HierarchyBasedAllocation`.
10. Wire the CLI to run a scenario and emit a run folder.
11. Add tests for reproducibility, scenario validation, model behavior, and metric calculations.
12. Remove the API weather forecast sample and replace it with run-inspection endpoints.

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
