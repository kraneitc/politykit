# Charterfall First Prototype Run Contract

This contract defines the first request and response path for the Charterfall prototype. It keeps Charterfall's game-layer choices separate from PolityKit's deterministic simulation inputs.

## Flow

```text
Select settlement + crisis + charter -> POST /api/runs -> GET /api/runs/{id}/dashboard -> POST /api/runs/{id}/rerun -> GET /api/runs/{id}/compare/{comparisonId}
```

The first prototype can hard-code one settlement, one crisis, one seed, one model, and one tick count while preserving the same contract shape for later clause composition.

## Field Sources

| Contract field | Source | First prototype value | Notes |
|---|---|---|---|
| `scenario` | Selected crisis | `village-food-crisis` | Comes from the crisis card or campaign chapter. Later chapters can use medicine shortage or corruption pressure scenarios. |
| `models` | Selected charter clauses or preset | `["need-based-allocation"]` | Comes from a `run-model` clause or preset-backed charter in [clauses.md](../design/clauses.md). |
| `seed` | Campaign state | `20260616` | Use a fixed seed for the first run so comparisons are legible. |
| `ticks` | Crisis definition or prototype tuning | `60` | Can be hard-coded until the first content pack fixes chapter lengths. |
| `parameters` | Approved clause parameters | `{ "needPriorityWeight": 1.0, "vulnerabilityPriorityWeight": 0.5 }` | Only include parameters that are mapped in the clause catalog. |
| game-layer clauses | Charter UI and campaign state | Stored outside PolityKit request | `game-layer-only` clauses affect copy, badges, unlocks, and inquiry framing, not deterministic simulation results. |
| campaign context | Game layer | Prior run IDs, chapter number, inquiry summary | Stored by Charterfall so the UI can advance, compare, and explain without changing PolityKit contracts. |

## Draft Run Creation

```http
POST /api/runs
```

Baseline allocation body:

```json
{
  "scenario": "village-food-crisis",
  "models": [
    "need-based-allocation"
  ],
  "seed": 20260616,
  "ticks": 60,
  "parameters": {
    "needPriorityWeight": 1.0,
    "vulnerabilityPriorityWeight": 0.5
  }
}
```

Preset-backed charter body:

```json
{
  "scenario": "village-food-crisis",
  "models": [
    "participatory-commons"
  ],
  "seed": 20260616,
  "ticks": 60
}
```

Expected use:

- The UI shows selected settlement, crisis, charter, seed, and assumptions before submission.
- The UI stores selected `game-layer-only` clauses in Charterfall state, not in `parameters`.
- The response creates a run ID that becomes the source for inquiry, rerun, and comparison.

## Inquiry

```http
GET /api/runs/{id}/dashboard
```

Use the dashboard response as the source for:

- five player-facing inquiry metrics
- event timeline
- collapse or severe-failure signals
- model summaries
- citizen story-card source facts where available
- public inquiry copy and outcome badges

The UI should translate dashboard data into playable feedback. A player should not need to read raw JSON to understand what happened.

## Amended Rerun

```http
POST /api/runs/{id}/rerun
```

Baseline amendment body:

```json
{
  "models": [
    "market-based-allocation"
  ],
  "parameters": {}
}
```

Parameter amendment body:

```json
{
  "models": [
    "need-based-allocation"
  ],
  "parameters": {
    "needPriorityWeight": 1.25,
    "vulnerabilityPriorityWeight": 0.5
  }
}
```

Rerun rules:

- Omit `models` to reuse the original run's model selection.
- Include `models` when a charter amendment changes the mapped PolityKit model or preset.
- Include only mapped parameters.
- Preserve the original scenario and seed unless the UI clearly labels a different experiment.
- Persist the new run ID as a comparison candidate in Charterfall campaign state.

## Comparison

```http
GET /api/runs/{id}/compare/{comparisonId}
```

Use comparison output to show before/after consequences:

- metric deltas
- changed severe-failure or collapse signals
- changed event patterns
- citizen or household story differences when source facts exist
- tradeoffs created by the amendment

Comparison copy should say "changed under this run" or "under these assumptions," not "proved better."

## Prototype UI Contract

The first UI should expose these player-facing states:

| State | Required player view | PolityKit surface |
|---|---|---|
| Draft | Settlement profile, crisis card, charter clauses, assumptions | Optional `GET /api/models`, `GET /api/scenarios`, `GET /api/metrics` |
| Crisis resolution | Run submitted and resolved | `POST /api/runs` |
| Inquiry | Metrics, events, harms, story cards, power-incentive notes | `GET /api/runs/{id}/dashboard` |
| Amendment | Clause changes and same-seed rerun confirmation | `POST /api/runs/{id}/rerun` |
| Comparison | Before/after deltas and explanatory copy | `GET /api/runs/{id}/compare/{comparisonId}` |
| Advance | Chapter number, prior run IDs, compact inquiry summary | Game-layer campaign state |

## Contract Boundaries

- The first prototype may hard-code `village-food-crisis`, `need-based-allocation`, `20260616`, and `60` ticks.
- The contract should still allow later selection of preset-backed charters such as `participatory-commons`, `regulated-market`, and `technocratic-administration`.
- `game-layer-only` clauses, citizen biographies, faction reactions, power-incentive badges, and campaign progress should not be treated as PolityKit simulation inputs until explicit mappings exist.
- AI text, if added later, must remain advisory and separate from deterministic run, dashboard, rerun, and comparison outputs.
