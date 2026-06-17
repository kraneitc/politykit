# Charterfall First Content Pack

This artifact defines the first settlement and three-crisis campaign for Charterfall's Milestone 1 prototype. It is design documentation for a compact, fictional content pack, not a claim about any real place or community.

## Settlement Profile

**Name:** Greywater Compact

**Premise:** Greywater Compact is a small settlement in a flood-cut basin where roads, river crossings, and supply wagons open and close with the season. Outside help exists, but it is late, partial, and never neutral, so the settlement survives by the rules it can enforce before the next route washes out. Its charter was drafted quickly after a series of evacuations, so the community has enough civic machinery to act together but not enough trust or capacity to absorb repeated shocks cleanly.

**Prototype population target:** 300-500 citizens.

This range is small enough for a compact prototype and citizen story cards, but large enough for scarcity to feel social rather than individual. The first run can use each scenario's current population until Charterfall-specific scenario files are authored.

## Starting Concerns

- **Food security:** stores are adequate in ordinary weeks but brittle under harvest or supply shocks.
- **Medicine access:** the clinic can handle routine care but struggles with triage, chronic illness, and sudden shortages.
- **Housing fragility:** temporary shelters and aging homes leave some households exposed to displacement.
- **Trust:** residents accept the charter, but prior emergency compromises make legitimacy fragile.
- **Administrative capacity:** appeals, ledgers, audits, and emergency orders all compete for limited staff time.

Greywater Compact should not imply any real-world place, ideology, ethnicity, religion, party, nation, or historical community. Factions, districts, citizen names, and public inquiry testimony should remain fictional.

## Isolation Premise

Greywater Compact is isolated enough that local institutions matter, but not so sealed that the world disappears. Traders, refugees, disease, inspectors, supply missions, rumors, and faction envoys can still arrive as crisis inputs. The important constraint is reliability: when a crisis hits, the settlement cannot assume that a distant authority, market, army, or relief caravan will arrive in time.

This premise supports the prototype by explaining:

- why a local charter can shape survival outcomes
- why scarcity recurs without requiring total apocalypse
- why outside help can be a scenario event rather than a guaranteed solution
- why citizen trust, appeals, ledgers, emergency powers, and faction pressure matter
- why later city-builder expansion can include roads, depots, flood seasons, envoys, and external obligations

## Prototype Crisis Order

| Order | Crisis | Simulation source | Existing seed | Ticks | Design purpose | Charter dimensions stressed |
|---:|---|---|---:|---:|---|---|
| 1 | Failed Harvest | `village-food-crisis` or `examples/village-food-crisis.json` | `12345` | `120` | Tests allocation pressure and visible unmet need. | Allocation method, transparency, administrative load, emergency powers. |
| 2 | Fever Season | `examples/medicine-shortage.json` | `24680` | `90` | Tests vulnerability, triage, and trust. | Allocation method, decision authority, accountability, appeal board, expert office. |
| 3 | Supply Office Scandal | `examples/corruption-stress.json` | `98765` | `80` | Tests transparency, accountability, legitimacy, and power incentives. | Transparency, accountability, audit office, citizen review, emergency powers, opacity. |

## Crisis Notes

### 1. Failed Harvest

Failed Harvest is the first crisis because it teaches the core loop with a familiar survival pressure: scarce food. It should make the player ask who receives scarce resources first and whether the charter reveals unmet need early enough to act.

Use this crisis to introduce:

- allocation clauses
- event timeline filtering
- needs met and severe failures
- first citizen story cards
- same-seed amendment and comparison

### 2. Fever Season

Fever Season adds triage and trust pressure. The player should see that a rule that worked for food may perform differently when medicine, vulnerability, and administrative bottlenecks matter more.

Use this crisis to introduce:

- expert office versus council framing
- appeal-board and audit-office consequences
- citizen testimony around illness, delay, and care
- trust movement after unmet health needs

### 3. Supply Office Scandal

Supply Office Scandal tests whether the charter can handle corruption pressure without collapsing into secrecy, patronage, or unchecked emergency control. It is the best first place to surface power-incentive hooks.

Use this crisis to introduce:

- public ledger versus delayed or closed reporting
- audit and citizen review story beats
- opacity, capture, accountability debt, and legitimacy strain notes
- comparison language that stays assumption-bound

## Seed Policy

Milestone 1 should use fixed scenario seeds by default:

- Failed Harvest: `12345`
- Fever Season: `24680`
- Supply Office Scandal: `98765`

Same-seed reruns should preserve the current crisis seed so the player can compare institutional changes without hidden randomness. The UI may display a single campaign seed later, but for the first prototype the simplest policy is fixed per-crisis seeds inherited from the current example scenarios.

If a prototype hard-codes the contract from [prototype-contract.md](../implementation/prototype-contract.md), it may initially use `20260616` for a single-crisis smoke path. The three-crisis content pack should move toward the fixed scenario seeds above once campaign flow exists.

## Carryover Policy

Milestone 1 carryover should be intentionally limited:

- Carry forward the campaign chapter number.
- Carry forward the selected charter and any amendments.
- Carry forward prior run IDs and comparison IDs.
- Carry forward compact inquiry summaries for player review.
- Carry forward visible warnings such as collapse risk, severe failures, trust strain, and power-incentive notes.
- Do not carry detailed citizen state, district state, resource inventories, or full world state between crises yet.

This keeps the first slice achievable while preserving the fantasy that the settlement remembers what happened. Later milestones can add persistent citizens, district conditions, timeline branches, and richer campaign state.

## First-Run Content Checklist

- One fictional settlement profile: Greywater Compact.
- Light isolation premise: flood-cut basin with intermittent outside contact.
- Three crisis cards: Failed Harvest, Fever Season, Supply Office Scandal.
- Scenario source or authoring task for each crisis.
- Stressed charter dimensions named for each crisis.
- Fixed seed policy.
- Limited carryover policy.
- No real-world scenario framing.
- No claim that a run proves one real-world institution is best.
