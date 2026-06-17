# Charterfall Win, Fail, And Carryover Rules

This artifact defines prototype-level win, fail, warning, and carryover rules for Charterfall's first three-crisis campaign. The goal is to make outcomes explainable before building UI or campaign state.

## Outcome Principle

Milestone 1 should not reduce the game to one universal score. A run can win with warnings, fail with useful evidence, or advance while carrying civic risk. The inquiry should explain what happened through metrics, events, citizen story cards, and power-incentive notes.

## Threshold Names

These threshold names should appear in prototype code, UI copy, and tuning notes even if exact values change later.

| Threshold name | PolityKit source | Default prototype threshold | Fail direction | Player-facing meaning |
|---|---|---:|---|---|
| Material survival floor | `Needs Met` | `< 0.5` | Too low | Too many citizens ended the crisis with basic needs unmet. |
| Trust floor | `Trust` | `< 25` | Too low | The settlement no longer broadly accepts the charter. |
| Severe harm ceiling | `Severe Failures` | `>= 10% population` | Too high | Too many people experienced severe harm or severe unmet need. |
| Administrative overload ceiling | `Administrative Load` | `>= 8` | Too high | The civic machinery is overloaded enough to threaten the response. |

These defaults mirror PolityKit's current failure-analysis criteria. Charterfall can tune them later, but changes should be documented and presented as game-balance choices, not proof claims.

## Fail States

A crisis or campaign can fail when any of these conditions are met:

- **Collapse detected**: PolityKit failure analysis reports a collapse event for a configured failure criterion.
- **Severe unmet need**: `Severe Failures` crosses the severe harm ceiling.
- **Trust breakdown**: `Trust` falls below the trust floor.
- **Material failure**: `Needs Met` falls below the material survival floor.
- **Administrative breakdown**: `Administrative Load` crosses the overload ceiling and the inquiry cannot plausibly describe recovery.

The UI should explain the specific reason, the metric involved, the threshold crossed, and the crisis events that contributed. Avoid generic "you lost" copy.

## Warning States

Warnings let the player advance while understanding that the settlement is fraying.

Recommended warning labels:

- **Needs strain**: needs met fell but did not cross the fail threshold.
- **Trust strain**: trust fell or stayed close to the floor.
- **Severe harm warning**: severe failures occurred but did not cross the ceiling.
- **Administrative pressure**: load rose sharply or approached overload.
- **Power precedent**: emergency exceptions, opacity, narrowed appeals, or faction bargains helped the run but created civic risk.
- **Unequal survival**: the settlement survived while inequality rose or harmed groups concentrated in a district, faction, or household type.

Warnings should carry into the next crisis as inquiry summaries and UI notes, not detailed simulation state.

## Win State

The first campaign win state is:

```text
The settlement survives all three crises while keeping Needs Met, Trust, and Severe Failures within documented bounds.
```

A win can still include warnings. For example, a player may survive all three crises with high administrative load, rising inequality, or power-incentive pressure. The final inquiry should show those tradeoffs rather than hiding them behind a clean victory badge.

## Advance Rules

After each crisis:

1. If a fail threshold is crossed, show the public inquiry and explain the failure.
2. If no fail threshold is crossed, allow the player to advance.
3. If warning thresholds or power-incentive hooks fired, show them before the amendment step.
4. Let the player amend the charter before the next crisis.
5. Store prior run IDs and comparison IDs so the player can review what changed.

Advancement should feel like governing through consequences, not clearing a level without residue.

## Milestone 1 Carryover

Carry forward:

- campaign chapter number
- selected charter and amendments
- prior run IDs
- comparison IDs
- compact inquiry summaries
- win/fail/warning labels
- final values for the five player-facing metrics
- power-incentive notes such as precedent, opacity, capture, accountability debt, or legitimacy strain
- player-facing explanation text generated from deterministic events and metrics

Do not carry forward yet:

- full world state
- detailed citizen state
- district inventories
- resource stockpiles
- relationship graphs
- arbitrary timeline branches
- hidden AI-generated facts

Each crisis should remain a deterministic PolityKit scenario run with campaign context attached by the Charterfall game layer.

## Explanation Requirements

Every outcome should answer:

- What happened?
- Which metric or event caused the result?
- Which charter choice or scenario pressure contributed?
- Who was helped, harmed, delayed, or ignored?
- What can the player amend before the next crisis?

Suggested copy patterns:

- "The settlement advances, but trust is strained under these assumptions."
- "The charter failed the Fever Season inquiry because severe failures crossed the documented threshold."
- "The emergency office prevented immediate collapse, but set a power precedent for the next crisis."
- "Needs met improved after the amendment, while administrative load increased."

Avoid:

- "This institution is best."
- "This ideology failed."
- "The correct charter is..."
- "The simulation proves..."

## Tuning Notes

- Start with PolityKit's default collapse thresholds for prototype clarity.
- Tune thresholds only after sample runs across all three crises.
- Keep win/fail thresholds visible to contributors before implementation.
- Keep player inquiry multi-metric even when a run has a single fail reason.
- Treat power-incentive carryover as game-layer state until explicit simulation mappings exist.
