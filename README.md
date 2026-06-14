# PolityKit

**PolityKit** is an open-source simulation framework for exploring how governance, allocation, and institutional systems behave under stress, scarcity, and changing assumptions.

It is currently a .NET scaffold for the simulation core, model libraries, metrics, scenarios, CLI, and API surface. The first implementation goal is a small, deterministic simulation that can compare several simple allocation models against the same scenario, seed, shocks, and metrics.

PolityKit does **not** attempt to prove which political, economic, or social system is best. It is an assumption laboratory:

> Given these assumptions, how does this system behave when conditions change?

## Current Status

The repository has been scaffolded, but the simulation engine and domain model are not implemented yet.

Built so far:

- `PolityKit.slnx` solution file.
- .NET 10 project structure under `src/`.
- Placeholder class library projects for core simulation concepts, engine logic, allocation models, metrics, and scenarios.
- A console app project for future command-line runs.
- An ASP.NET Core API project with the default weather forecast sample still present.
- Empty top-level `examples/` and `tests/` folders reserved for future scenarios and automated tests.
- Apache 2.0 license.

Not built yet:

- Simulation loop.
- World state, citizen, resource, event, and scenario types.
- Allocation model interfaces or implementations.
- Metrics calculations.
- Scenario loading.
- Run output files.
- CLI commands beyond the default "Hello, World!" program.
- Real API endpoints beyond the default scaffold.
- Tests.

## Repository Layout

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

### Project Roles

- `PolityKit.Sim.Core`: shared domain types and contracts, such as world state, citizens, resources, decisions, events, and common configuration.
- `PolityKit.Sim.Engine`: deterministic simulation orchestration, tick execution, world-rule application, shock handling, and run lifecycle.
- `PolityKit.Sim.Models`: allocation and governance model implementations, beginning with simple mechanical models.
- `PolityKit.Sim.Metrics`: metric definitions and calculators for comparing runs.
- `PolityKit.Sim.Scenarios`: scenario definitions, loading, validation, and bundled starter scenarios.
- `PolityKit.Sim.Cli`: command-line runner for local simulations and output generation.
- `PolityKit.Sim.Api`: HTTP API surface for future dashboards, tools, and integrations.

## Development

Prerequisite:

- .NET 10 SDK.

Build the solution:

```bash
dotnet build PolityKit.slnx
```

Run the CLI scaffold:

```bash
dotnet run --project src/PolityKit.Sim.Cli
```

Run the API scaffold:

```bash
dotnet run --project src/PolityKit.Sim.Api
```

The CLI and API currently expose only the default scaffold behavior. They are placeholders for the simulation runner and future API endpoints.

## Intended First Prototype

The first useful version should stay intentionally small:

- One simple world model.
- A small population model.
- Three resources: food, medicine, and housing.
- Three allocation models:
  - `NeedBasedAllocation`
  - `MarketBasedAllocation`
  - `HierarchyBasedAllocation`
- One starter scenario: village food crisis.
- Deterministic runs from a seed.
- CSV and JSONL output.
- Basic side-by-side metrics.

A future CLI run may look like:

```bash
politykit run \
  --scenario examples/village-food-crisis.json \
  --models need-based,market-based,hierarchy-based \
  --seed 12345 \
  --ticks 120
```

Suggested output:

```text
runs/village-food-crisis-12345/
  config.json
  metrics.csv
  events.jsonl
  citizens-final.csv
  summary.json
```

## Core Concepts

### World State

`WorldState` will represent the current simulation state. It is expected to include the current tick, population, resources, institutional state, environmental conditions, trust, inequality, and recorded events.

### Citizens

Citizens are simulated agents with simple properties such as need, health, wealth, trust, vulnerability, social power, and access to resources.

### Resources

Initial resources are expected to include food, medicine, housing, administrative capacity, and production capacity.

### System Models

A system model decides how a simulated society responds to current conditions. Early models should use neutral mechanical names rather than broad ideological labels.

Examples:

- `NeedBasedAllocation`
- `MarketBasedAllocation`
- `HierarchyBasedAllocation`
- `CentralCommandAllocation`
- `VoteWeightedAllocation`

### Scenarios

A scenario defines starting conditions and scheduled shocks.

Examples:

- Food shortage.
- Medicine shortage.
- Administrative overload.
- Corruption spike.
- Loss of institutional capacity.
- Population growth.
- Information failure.

### Metrics

Metrics describe what happened during a run.

Initial candidates:

- Needs met.
- Severe failures.
- Trust.
- Inequality.
- Resource waste.
- Administrative load.
- Recovery time.

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

The engine should define shared world mechanics. System models should decide what actions to take within that world.

For example:

- The engine defines what happens when a citizen does not receive enough food.
- A system model decides who receives food.

This separation keeps comparisons consistent.

### Reproducibility Matters

Given the same scenario, seed, model, configuration, and version, a run should produce the same result.

Every run should record:

- Scenario name.
- Model name and version.
- Configuration values.
- Random seed.
- Simulation duration.
- Metrics.
- Event log.

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

Early contribution areas include:

- Domain types and simulation contracts.
- Deterministic engine loop.
- Starter allocation models.
- Metrics.
- Stress scenarios.
- Event logging.
- CLI runner.
- Tests.
- Documentation for assumptions and model limitations.

The project should welcome disagreement by turning it into inspectable model-building: fork the model, change the assumptions, run the same scenario, and compare the results.

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md).

## License

PolityKit is licensed under the Apache License 2.0. See [LICENSE](LICENSE).

## Disclaimer

PolityKit is a simulation framework for exploring assumptions and system behavior. It should not be used as a source of political, economic, legal, or policy authority. Models are simplified by design and should be interpreted as experimental tools rather than representations of real societies.
