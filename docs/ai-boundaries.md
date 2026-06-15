# AI Boundaries And Safety

PolityKit simulations must remain runnable, reproducible, and interpretable without AI configuration.

## Boundary Rules

- AI analysis is optional and is never required to run simulations.
- AI output is advisory text or proposed artifacts, not authoritative simulation data.
- Simulation data comes from deterministic run, sweep, stress, comparison, metric, event, and summary outputs.
- AI features must not change model decisions, world rules, metric calculations, seeds, scenarios, or stored run results.
- AI provenance must be recorded beside AI-assisted artifacts so readers can see what inputs were read and which provider/model generated the text.

## Provenance Shape

AI-assisted artifacts should include an `aiAnalysis` record using the shared `AiAnalysisUsage` shape:

```json
{
  "used": true,
  "inputRunIds": [
    "11111111-1111-1111-1111-111111111111"
  ],
  "inputFiles": [
    "runs/example/summary.json"
  ],
  "providerName": "example-provider",
  "providerModel": "example-model",
  "promptTemplateVersion": "run-summary-v1",
  "createdAt": "2026-06-15T00:00:00+00:00",
  "boundaryRule": "AI output is advisory text or proposed artifacts, not authoritative simulation data."
}
```

When no AI analysis was used, outputs should record:

```json
{
  "used": false,
  "inputRunIds": [],
  "inputFiles": [],
  "providerName": null,
  "providerModel": null,
  "promptTemplateVersion": null,
  "createdAt": null,
  "boundaryRule": "AI output is advisory text or proposed artifacts, not authoritative simulation data."
}
```

## Privacy Note

Run data sent to external AI providers may include scenario names, model names and versions, seeds, parameters, metric values, event descriptions, and final citizen state exports. Treat those payloads as data shared outside the local process.

Before sending run data to an external provider:

- Review the generated context or artifact input.
- Exclude or redact fields that should not leave the local environment.
- Do not include API keys, local secrets, or private notes in prompt inputs.
- Record the provider name, provider model, prompt template version, input run IDs or files, and creation time.

The default local behavior is no AI analysis. CLI run, sweep, and stress workflows write an `ai-analysis.json` sidecar with `used: false`, and API workflow responses include `aiAnalysis.used: false` unless a future explicit AI feature creates an advisory artifact.
