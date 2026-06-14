# Golden Interpreted Run

This folder is a small, checked-in example of a readable PolityKit run bundle.

It was generated from [../interpretability-demo.json](../interpretability-demo.json) with:

```powershell
dotnet run --project src/PolityKit.Sim.Cli -- run `
  --scenario examples/interpretability-demo.json `
  --models need-based-allocation `
  --seed 4242 `
  --ticks 12 `
  --out examples/golden-interpreted-run
```

Start with [summary.json](summary.json). It shows final metrics, event counts, and `NotableMetricChanges` with breadcrumb text linking metric movement to nearby events.

The supporting files mirror normal CLI output:

- [config.json](config.json): scenario, model, metric, seed, and parameter details.
- [metrics.csv](metrics.csv): metric values by tick.
- [events.jsonl](events.jsonl): event stream with richer context fields.
- [citizens-final.csv](citizens-final.csv): final citizen state.
