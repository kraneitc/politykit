# Contributing To PolityKit

PolityKit welcomes contributions that make assumptions easier to inspect, compare, and test.

The project is intentionally small at this stage. A good contribution should keep the simulation reproducible, make trade-offs explicit, and avoid turning a model result into a real-world claim.

## Good First Contribution Areas

- New scenario files under `examples/`.
- New metrics under `src/PolityKit.Sim.Metrics/`.
- New allocation model variants under `src/PolityKit.Sim.Models/`.
- Tests that document expected behavior.
- Documentation for assumptions, formulas, limitations, and run interpretation.

## Contributor Guides

- [How to add a model](add-model.md)
- [Governance presets](governance-presets.md)
- [How to add a metric](add-metric.md)
- [How to add a scenario](add-scenario.md)
- [Scenario format reference](scenarios.md)

## Development Loop

Build the solution:

```bash
dotnet build PolityKit.slnx
```

Run the tests:

```bash
dotnet test PolityKit.slnx
```

Run a local comparison after changing models, metrics, or scenarios:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run \
  --scenario examples/village-food-crisis.json \
  --seed 12345 \
  --ticks 60 \
  --out runs/contributor-check
```

On Windows PowerShell:

```powershell
dotnet run --project src/PolityKit.Sim.Cli -- run `
  --scenario examples/village-food-crisis.json `
  --seed 12345 `
  --ticks 60 `
  --out runs/contributor-check
```

Inspect `summary.json`, `metrics.csv`, and `events.jsonl` before opening a change.

## Contribution Standards

### Keep Assumptions Visible

Models should expose their purpose, assumptions, defaults, and known failure modes through a `ModelManifest`.

Metrics should document what they measure, what a higher value means, and any important blind spots.

Scenarios should explain the stress pattern they are trying to exercise through clear names, resource levels, and shocks.

### Preserve Reproducibility

Given the same scenario, seed, model list, model parameters, and tick count, a run should produce the same result.

Avoid wall-clock time, unseeded randomness, external services, hidden local state, or nondeterministic ordering in simulation logic.

AI-assisted analysis must remain outside the deterministic simulation path. Do not require AI configuration for run, sweep, stress, or comparison workflows, and do not let AI output modify model decisions, world rules, metrics, or stored run results. See [AI boundaries and safety](ai-boundaries.md).

### Keep The Engine Neutral

System-specific behavior belongs in model implementations. Shared mechanics belong in the engine and world rules.

Before changing engine behavior, ask whether the change applies fairly to every model. If it only describes one system's preference, it probably belongs in a model.

### Test The Contract

Add or update tests when a contribution changes behavior:

- Model contributions should cover catalog registration, manifest fields, parameter handling, and key decision behavior.
- Metric contributions should cover empty worlds, representative worlds, event-sensitive behavior if applicable, and null guard behavior.
- Scenario contributions should pass schema and validator tests and should run through the CLI.

## Pull Request Checklist

- The solution builds.
- Tests pass, or any failing tests are explained.
- New models are registered in `DefaultModelSet`.
- New metrics are registered in `DefaultMetricSet`.
- New scenarios are valid JSON and pass the scenario schema expectations.
- Assumptions and limitations are documented.
- Example outputs, if included, were produced with a fixed seed.

## Language And Claims

PolityKit should compare model behavior under declared assumptions. Avoid wording that says a model proves how a real society works.

Prefer:

```text
Under this scenario and these assumptions, the model preserved trust longer.
```

Avoid:

```text
This system is better.
```
