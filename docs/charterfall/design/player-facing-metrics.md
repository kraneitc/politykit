# Charterfall Player-Facing Metrics

This artifact defines the five default metrics for Charterfall's first public inquiry screen. These are player-facing labels over existing PolityKit dashboard metrics, not new simulation claims.

## Default Inquiry Metrics

| Metric role | Player-facing label | PolityKit metric name | Direction | Purpose |
|---|---|---|---|---|
| Survival | Needs met | `Needs Met` | Higher is better | Shows whether the settlement materially endured the crisis. |
| Harm | Severe failures | `Severe Failures` | Lower is better | Shows who was failed badly enough to matter narratively. |
| Legitimacy | Trust | `Trust` | Higher is better | Shows whether people still accept the system. |
| Equity | Inequality | `Inequality` | Lower is better | Shows distributional tradeoffs. |
| Friction | Administrative load | `Administrative Load` | Lower is usually better | Shows the cost of process, transparency, review, and emergency administration. |

## Player Guidance

**Needs met** tells the player how many citizens ended the crisis with basic needs satisfied. A high value means the settlement materially held together, but it does not guarantee that the process was trusted, fair, or sustainable.

**Severe failures** tells the player where the system failed people badly enough to become a public inquiry concern. This metric should drive citizen story cards, harmed-group summaries, and collapse warnings.

**Trust** tells the player whether citizens and institutions still believe the charter is working. Trust can fall even when material survival improves, especially when authority is concentrated, information is hidden, or appeals go unresolved.

**Inequality** tells the player whether outcomes are unevenly distributed. It should help distinguish "the settlement survived" from "the settlement survived by protecting some groups and exposing others."

**Administrative load** tells the player how much civic machinery the system had to carry. High load can mean appeals, audits, transparency, or emergency coordination are stressing the settlement's capacity.

## Data Sources

| UI surface | API source | Use |
|---|---|---|
| Public inquiry summary | `GET /api/runs/{id}/dashboard` | Show final values for the five default metrics. |
| Event timeline | `GET /api/runs/{id}/dashboard` | Link metric movement to shocks, allocations, severe failures, administrative backlog, and trust events. |
| Before/after comparison | `GET /api/runs/{id}/compare/{comparisonId}` | Show metric deltas after an amendment or rerun. |
| Advanced/debug details | `GET /api/metrics` | Confirm raw PolityKit metric names and developer-facing descriptions. |

Raw PolityKit metric names should remain available in tooltips, debug panels, or developer documentation. The player-facing inquiry should use the labels above.

## Comparison Copy

Comparison views should show deltas, not a single universal score.

Recommended language:

- "Needs met increased under this run."
- "Severe failures fell after the amendment."
- "Trust changed under these assumptions."
- "Administrative load rose while appeals were available."
- "Inequality moved in the amended run."

Avoid:

- "This charter is better."
- "This institution is superior."
- "This proves the system works."
- "This is the correct ideology."

## Win And Fail Use

Milestone 1 may use thresholds for win and fail states, but the inquiry should remain multi-metric.

Recommended fail signals:

- collapse detected by scenario or failure criteria
- severe failures above a documented threshold
- trust below a documented threshold
- needs met below a documented threshold

Recommended win signal:

- the settlement survives all three crises while keeping needs met, trust, and severe failures within documented bounds

Even when a run wins, the inquiry should still show tradeoffs. A settlement can survive with high administrative load, high inequality, low trust, unresolved severe failures, or power-incentive pressure.

## UI Notes

- Show all five metrics on the first inquiry screen.
- Use a compact visual state for direction: improved, worsened, unchanged, or mixed.
- Pair metric movement with events and citizen story cards where possible.
- Do not hide bad secondary metrics behind a win badge.
- Do not collapse the five metrics into one civic score in Milestone 1.
- Use "under these assumptions" language for comparisons.
