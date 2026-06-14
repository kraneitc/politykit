# PolityKit Example Scenarios

This folder contains starter scenario JSON files for local simulation runs.

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

Example future CLI usage:

```bash
politykit run --scenario examples/village-food-crisis.json --seed 12345 --ticks 120
```
