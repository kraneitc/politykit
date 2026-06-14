# PolityKit Example Scenarios

This folder contains starter scenario JSON files for local simulation runs.

It also contains [golden-interpreted-run](golden-interpreted-run), a compact checked-in run bundle that demonstrates readable interpreted output.

Each file follows the current `ScenarioDefinition` shape:

- `name`: scenario display name.
- `seed`: deterministic seed used when a run does not override it.
- `ticks`: number of simulation ticks.
- `initialPopulation`: number of generated citizens.
- `initialResources`: starting resource pool.
- `shocks`: scheduled disruptions applied at specific ticks.

Known shock types currently handled by the engine include:

- `CropFailure`
- `MedicineShortage`
- `HousingLoss`
- `AdministrativeOverload`
- `AdminLoss`
- `CorruptionSpike`

The golden bundle was generated from `interpretability-demo.json` and is useful when changing summary or event-log behavior.

Example future CLI usage:

```bash
politykit run --scenario examples/village-food-crisis.json --seed 12345 --ticks 120
```
