# Governance Presets

Governance presets are reusable bundles of governance dimensions for comparing model behavior under declared assumptions.

They are not descriptions of real societies. They are simplified experimental configurations that help users ask:

```text
Given these assumptions, how does this bundle behave under this scenario?
```

They should not be read as:

```text
This label accurately represents a real political, economic, or social system.
```

## Presets Versus Baseline Models

Baseline allocation models are mechanical decision rules:

- `NeedBasedAllocation`
- `MarketBasedAllocation`
- `HierarchyBasedAllocation`

Composite governance presets are configured `CompositeGovernance:<preset-id>` models. Each preset combines dimensions such as allocation mechanism, decision authority, accountability, information flow, property regime, and appeal process.

Use a baseline model when you want to test one allocation mechanism directly. Use a governance preset when you want to test how several declared governance assumptions interact.

## Current Presets

The current preset catalog includes:

- `participatory-commons`
- `regulated-market`
- `central-planning`
- `patronage-hierarchy`
- `mutual-aid-federation`
- `technocratic-administration`

These labels are short handles for bundles of assumptions. Their manifests are the source of truth for what they mean inside the simulation.

## What A Preset Manifest Explains

Each preset-backed model exposes:

- The preset ID and display name.
- Preset-level assumptions.
- Preset-level known failure modes.
- Selected governance dimensions.
- Per-dimension assumptions and failure modes.
- Any dimension parameters used by the composite model.

Read the manifest before interpreting results. The label alone is not enough.

## Interpretation Boundaries

When comparing presets, keep these boundaries in view:

- Presets are simplified bundles of assumptions, not claims about real societies.
- Results depend on the selected scenario, seed, metrics, model version, and parameters.
- Higher or lower metric values are simulation diagnostics, not policy judgments.
- `modelRobustness` compares behavior under the tested grid, not universal resilience.
- A preset can perform well in one stress pattern and poorly in another.
- Missing dimensions or simplified world rules can matter as much as visible assumptions.

Prefer language like:

```text
Under the medicine shortage scenario, this preset had fewer severe failures than the baseline model.
```

Avoid language like:

```text
This kind of society is better.
```

## Comparison Workflow

A useful comparison includes at least one baseline model and one preset:

```bash
dotnet run --project src/PolityKit.Sim.Cli -- run \
  --scenario examples/village-food-crisis.json \
  --models need-based-allocation,market-based-allocation,hierarchy-based-allocation,participatory-commons \
  --seed 20260614 \
  --ticks 60 \
  --out runs/food-crisis-baseline-preset-compare
```

Stress comparisons can mix baseline names and preset IDs:

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

Inspect `stress-summary.json`, especially `modelRobustness`, sensitivity reports, collapse events, and the final metrics that drove the comparison.
