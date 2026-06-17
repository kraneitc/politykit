# Charterfall Milestone 1: Prototype Product Framing

Milestone 1 turns the Milestone 0 product framing into a playable text-and-dashboard prototype. It should prove the core Charterfall loop with the smallest useful UI: draft a charter, resolve a crisis through PolityKit, inspect an inquiry, amend the charter, compare the amended run, and advance through a short campaign.

This milestone is implementation work, but the product target is still deliberately narrow. It should make the institutional survival loop playable before adding richer game feel, spatial city building, procedural narrative, or deep citizen simulation.

Source roadmap section: `Milestone 1: Charterfall Prototype` in [charterfall-roadmap.md](../charterfall-roadmap.md).

## Goal

Create a playable Charterfall prototype that lets a player complete one three-crisis run from draft to final outcome.

The prototype should answer one question:

```text
Is it fun to draft institutions, watch them fail or adapt under crisis, and revise them based on inspectable consequences?
```

By the end of this milestone:

- A player can select a scenario and charter, run the simulation, and understand the outcome without reading raw JSON.
- A player can amend at least one mapped charter choice and compare the amended run against the original under the same declared assumptions.
- The UI separates deterministic PolityKit outputs from Charterfall presentation, campaign state, and game-layer-only clauses.
- The prototype is sufficient for Milestone 2 to improve game feel, readability, citizen stories, reactions, and tutorialization.

## Product Boundary

Milestone 1 is the first playable Charterfall slice, not the full Constitutional Survival City Builder. PolityKit remains the deterministic simulation layer. Charterfall is responsible for player flow, presentation, campaign state, clause selection, inquiry screens, comparison language, and fiction.

The prototype should use the Milestone 0 artifacts as design inputs:

- [Charterfall pitch](../../design/charterfall-pitch.md)
- [Clause catalog](../../design/clauses.md)
- [First content pack](../../design/first-content-pack.md)
- [Player-facing metrics](../../design/player-facing-metrics.md)
- [Win, fail, and carryover rules](../../design/win-fail-carryover.md)
- [Framing and claims boundary](../../design/framing-boundaries.md)
- [First prototype run contract](../prototype-contract.md)

## Product Shape

The prototype should feel like a compact civic lab, not a full city builder. It can be a lightweight web app or desktop UI, but it should expose the whole loop as player-facing screens rather than as developer tools.

Recommended target if no Charterfall app exists yet:

```text
src/Charterfall.App
```

Required player states:

| State | Player purpose | Source artifact |
|---|---|---|
| Draft | Choose settlement, crisis, and charter assumptions. | [charterfall-pitch.md](../../design/charterfall-pitch.md), [clauses.md](../../design/clauses.md) |
| Resolve | Submit the run and wait for deterministic results. | [prototype-contract.md](../prototype-contract.md) |
| Inquiry | Inspect metrics, events, harms, and explanatory summary. | [player-facing-metrics.md](../../design/player-facing-metrics.md) |
| Amend | Change a mapped clause or parameter after seeing consequences. | [clauses.md](../../design/clauses.md) |
| Compare | View original and amended outcomes side by side. | [prototype-contract.md](../prototype-contract.md) |
| Advance | Carry compact outcome state into the next crisis. | [first-content-pack.md](../../design/first-content-pack.md), [win-fail-carryover.md](../../design/win-fail-carryover.md) |

## Deliverables

### 1. Lightweight Prototype Shell

Build the first playable shell as a small Charterfall app surface that calls existing PolityKit APIs or equivalent local run services.

The shell should include:

- A persistent session state for the current campaign chapter.
- A visible route or view for Draft, Inquiry, Amendment, Comparison, and Final Outcome.
- A run history panel or compact list showing original and amended run IDs.
- Clear loading, error, and empty states for failed API calls or unavailable scenario data.

Definition of done:

- The app can be launched locally by a contributor.
- The first screen starts inside the playable experience, not a marketing page.
- A player can move through the required states without editing files or invoking CLI commands.

### 2. Scenario Selection

Expose crisis selection using the first content pack.

Minimum scenario set:

| Chapter | Crisis | Scenario source | Seed | Ticks |
|---:|---|---|---:|---:|
| 1 | Failed Harvest | `village-food-crisis` or `examples/village-food-crisis.json` | `12345` | `120` |
| 2 | Fever Season | `examples/medicine-shortage.json` | `24680` | `90` |
| 3 | Supply Office Scandal | `examples/corruption-stress.json` | `98765` | `80` |

Definition of done:

- The player can see the selected settlement and crisis before running.
- The selected scenario, seed, tick count, and assumptions are visible before submission.
- The first implementation may lock chapter order, but the UI should still name the active crisis explicitly.

### 3. Charter Clause Selection

Let the player choose a small charter surface before the first crisis and amend it after inquiry.

Minimum first-run charter surface:

- Allocation method as the first authoritative simulation choice.
- One or two additional dimensions as clearly labeled game-layer presentation choices, if composition is not implemented yet.
- Stable clause IDs from the clause catalog.
- Tradeoff text for every visible clause.
- Boundary copy that keeps choices fictional and assumption-bound.

Definition of done:

- At least one selected clause maps to a PolityKit `run-model` or approved parameter.
- `game-layer-only` clauses are stored in Charterfall state, not sent as authoritative PolityKit parameters.
- The amendment screen prevents unmapped clauses from pretending to change deterministic simulation results.

### 4. API Integration For Run Creation

Wire Draft to deterministic run creation.

The prototype should use this flow:

```text
Select settlement + crisis + charter -> POST /api/runs
```

The request should include:

- `scenario`
- `models`
- `seed`
- `ticks`
- mapped `parameters`, when applicable

Definition of done:

- The UI can create a run and persist the returned run ID.
- The submitted request is inspectable through a debug panel, developer console, or saved run summary.
- Run creation uses only mapped model IDs, preset IDs, or parameters from the clause catalog.

### 5. Dashboard Inquiry View

Turn dashboard output into a playable inquiry screen.

Required inquiry content:

- Five player-facing metrics: Needs Met, Severe Failures, Trust, Inequality, and Administrative Load.
- Event timeline or event list.
- Severe-failure, collapse, or warning signals where available.
- A concise explanation of what changed and who was exposed.
- A visible assumptions block listing scenario, seed, model, parameters, and chapter.

Definition of done:

- A player can understand the run outcome without reading raw JSON.
- Metrics use player-facing names while remaining traceable to dashboard fields.
- Inquiry copy says "under these assumptions" or equivalent bounded language for comparisons and causal explanations.

### 6. Rerun With Amended Clauses

Allow the player to amend the charter and rerun from the same starting conditions.

The prototype should use:

```text
POST /api/runs/{id}/rerun
```

Rerun rules:

- Preserve scenario and seed by default.
- Include changed `models` only when the amendment changes an authoritative mapped model or preset.
- Include changed `parameters` only when they are documented in the clause catalog.
- Persist the amended run ID as a comparison candidate.

Definition of done:

- A player can create at least one amended run from the inquiry screen.
- The UI makes same-seed rerun behavior explicit.
- The amendment screen distinguishes simulation changes from presentation-only charter notes.

### 7. Comparison View

Compare original and amended runs as first-class gameplay.

The prototype should use:

```text
GET /api/runs/{id}/compare/{comparisonId}
```

Required comparison content:

- Before/after values for the five player-facing metrics.
- Clear indicator of which charter choice changed.
- Changed warning, severe-failure, or collapse signals where available.
- Short tradeoff summary.
- A button or path to continue with either the original or amended charter state.

Definition of done:

- The player can compare an amended run against the original without leaving the app.
- Copy avoids "proved better" language and uses assumption-bound phrasing.
- Comparison data remains traceable to deterministic run IDs.

### 8. Three-Crisis Campaign Script

Implement the first campaign as a compact scripted sequence using the first content pack.

Campaign state should track:

- settlement ID
- chapter number
- active crisis ID
- selected charter clause IDs
- authoritative PolityKit run IDs
- selected continuation run ID
- compact inquiry summary
- limited carryover flags from win/fail/carryover rules

Definition of done:

- A player can complete three crises in order.
- The final outcome uses documented win, fail, warning, and carryover rules.
- Carryover remains intentionally limited and game-layer-owned unless a simulation mapping exists.

## Scope Boundaries

Milestone 1 should not build:

- full spatial city construction
- procedural citizen life simulation
- procedural campaign generation
- advanced stress, sweep, robustness, or sensitivity views as core flow
- freeform clause composition beyond documented mappings
- AI-generated inquiry, citizen testimony, or scenario suggestions
- real-world political ranking, ideology scoring, or claims of institutional superiority

These can become later work only after the basic loop is playable and readable.

## Claims Boundary

Charterfall shows how fictional institutional rules behave inside declared simulation assumptions. It does not prove that a real-world political, economic, or social system is superior.

The prototype must keep this boundary visible in:

- Draft assumptions.
- Inquiry explanations.
- Amendment confirmation.
- Comparison copy.
- Final outcome summary.
- Any debug or exported run summaries intended for players.

## Artifact Checklist

Create or update these artifacts during Milestone 1:

| Artifact | Suggested path | Required before Milestone 2? |
|---|---|---:|
| Prototype product framing | `docs/charterfall/implementation/milestone-1/milestone-1-product-framing.md` | Yes |
| Prototype app entrypoint | To be chosen by implementation | Yes |
| Scenario selection UI notes | In prototype app docs or implementation notes | Yes |
| Charter clause UI notes | In prototype app docs or implementation notes | Yes |
| API integration notes | In prototype app docs or implementation notes | Yes |
| Inquiry screen notes | In prototype app docs or implementation notes | Yes |
| Comparison screen notes | In prototype app docs or implementation notes | Yes |
| Campaign state notes | In prototype app docs or implementation notes | Yes |
| Launch and verification instructions | In prototype README or root app docs | Yes |

These paths can change once the app structure exists. Keep artifact names and responsibilities stable even if implementation files move.

## Acceptance Verification

Milestone 1 is complete when the following checks pass:

1. A contributor can launch the prototype locally from documented instructions.
2. A player can complete the loop: draft, resolve, inquire, amend, compare, and advance.
3. A player can complete the three-crisis campaign from the first content pack.
4. At least one amendment changes an authoritative PolityKit model, preset, or parameter.
5. The inquiry screen shows the five player-facing metrics and an event timeline or event list.
6. The comparison screen shows before/after consequences for original and amended runs.
7. Game-layer-only clauses are not sent as deterministic simulation inputs.
8. Outcome copy remains fictional, assumption-bound, and aligned with the claims boundary.

## Open Decisions

These should be closed early in Milestone 1:

- Where the prototype app should live in the repository.
- Whether the first app surface is web, desktop, or hosted inside an existing app.
- Whether API integration calls a running PolityKit service or an in-process/local runner.
- Which allocation models or presets are exposed in the very first playable build.
- Whether chapters 2 and 3 are selectable immediately or unlocked after chapter completion.
- What local verification command proves the prototype can complete a draft-to-compare run.
