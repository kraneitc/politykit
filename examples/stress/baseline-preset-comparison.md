# Baseline And Preset Stress Comparison

This example compares mechanical baseline models with composite governance presets across existing challenge scenarios. Presets are simplified bundles of experimental assumptions, not claims about real societies.

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

Inspect `runs/baseline-preset-comparison-stress/stress-summary.json` after the run. The `modelRobustness` array uses the same shape for baseline models and preset-backed composite models:

```json
{
  "model": "CompositeGovernance:regulated-market",
  "scenariosTested": ["Corruption Stress", "Medicine Shortage"],
  "seedsTested": [111, 222],
  "runsCompleted": 12,
  "mostSensitiveParameter": "needWeightMultiplier"
}
```
