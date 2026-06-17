# Charterfall Political Framing And Claims Boundary

This artifact defines how Charterfall should talk about institutions, simulation results, scenarios, citizen stories, comparisons, and any future AI-assisted presentation text.

## Required Claims Boundary

```text
Charterfall shows how fictional institutional rules behave inside declared simulation assumptions. It does not prove that a real-world political, economic, or social system is superior.
```

Use this wording in Milestone 0 artifacts, public-facing design notes, and any prototype screen that explains what a comparison means.

## Where This Boundary Applies

- Game design brief.
- Pitch.
- Clause definitions.
- Public inquiry copy.
- Comparison screens.
- Scenario authoring notes.
- Citizen story cards.
- Win, fail, and carryover explanations.
- Power-incentive notes.
- Any future AI-assisted summaries, testimony, or scenario suggestions.

## Framing Rules

| Rule | Use | Avoid |
|---|---|---|
| Use fictional settings | Greywater Compact, fictional factions, fictional households, fictional offices | Real parties, nations, religions, ethnicities, or historical communities as direct targets |
| Treat labels as experiment handles | "need-based allocation under these assumptions" | "need-based allocation proves this real policy is best" |
| Make assumptions visible | show scenario, seed, model, metrics, thresholds, and selected clauses | hiding assumptions behind a victory badge |
| Keep comparisons bounded | "changed under this run" or "under these assumptions" | "proved better", "superior", "correct ideology" |
| Separate simulation from presentation | deterministic metrics/events drive inquiry, story cards dramatize those facts | invented citizen harms, invented model effects, AI-created facts |
| Keep AI advisory | optional text with provenance, never simulation truth | AI summaries as authoritative outcomes or policy recommendations |

## Recommended Copy Patterns

- "Under these assumptions, the amended charter reduced severe failures but increased administrative load."
- "This run shows how Greywater Compact behaved with this seed, crisis, and charter."
- "The inquiry found a trust loss after the corruption shock."
- "The appeal board corrected some harms, but the process added administrative pressure."
- "The emergency office helped the settlement act quickly, but it set a precedent for future bypasses."
- "This citizen story is grounded in the run's events and metrics."

## Avoided Copy Patterns

- "This proves one institution is superior."
- "This ideology failed."
- "The correct charter is..."
- "The simulation predicts what would happen in the real world."
- "AI says this policy is best."
- "The model discovered the truth about society."

## Deterministic Data Boundary

PolityKit remains the source of deterministic simulation facts:

- scenario names and scenario files
- seeds
- model and preset IDs
- model manifests and parameters
- metrics
- events
- run dashboards
- reruns
- comparisons
- stress, sweep, sensitivity, robustness, and collapse summaries

The Charterfall game layer may turn those facts into:

- public inquiry screens
- citizen story cards
- faction reactions
- outcome badges
- power-incentive notes
- campaign summaries
- tutorial text
- fictional framing

The game layer should not imply that presentation text is the simulation source of truth. If a story says a household was harmed, the UI should be able to trace that statement back to metrics, events, dashboard data, or documented game-layer state.

## AI Boundary

AI-generated text, if used later, must remain optional and advisory.

AI may help draft:

- inquiry summaries
- citizen testimony variants
- faction reaction text
- scenario-draft suggestions
- model or clause critique prompts

AI must not:

- change run results
- change model behavior
- change metrics
- change scenario validation
- invent authoritative citizen outcomes
- rank real-world ideologies, countries, communities, or institutions
- present generated text as proof

Any AI-assisted artifact should cite deterministic source inputs such as run IDs, scenario names, model names, seeds, metric names, event references, or comparison IDs.

## Scenario Authoring Boundary

Scenario authors should:

- use fictional settlements and fictional factions
- describe pressures as crisis conditions, not real-world verdicts
- document assumptions before outcomes
- keep scenario seeds fixed for reproducibility
- state what charter dimensions the scenario is meant to stress
- avoid real-world place names, parties, communities, or direct historical reenactments in the core prototype

Historically inspired scenario packs may exist later, but they should remain abstract, assumption-bound, and clearly separated from claims about real history.

## Comparison Boundary

Comparison screens should show tradeoffs across multiple metrics and stories.

They may say:

- "Needs met increased."
- "Trust fell."
- "Administrative load rose."
- "Severe failures dropped."
- "This citizen story changed between runs."

They should not say:

- "This charter is objectively better."
- "This institution wins."
- "This result proves the system."

## Contributor Checklist

Before adding Charterfall copy, scenarios, UI text, or generated summaries, check:

1. Is the setting fictional?
2. Are assumptions visible?
3. Are deterministic facts separate from interpretation?
4. Does comparison language use "under these assumptions" or equivalent wording?
5. Does the copy avoid ranking real-world groups, ideologies, states, or institutions?
6. If AI was used, is it marked advisory and tied to deterministic source inputs?
