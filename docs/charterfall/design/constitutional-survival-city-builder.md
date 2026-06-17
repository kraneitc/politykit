# Constitutional Survival City Builder

## 1. Product Description

The Constitutional Survival City Builder is a long-term settlement management game about keeping a fragile community alive by designing the institutions that govern it. The player manages food, shelter, labor, legitimacy, infrastructure, and ecological pressure, but their most important tools are the civic rules that decide how scarce resources are allocated, who has authority during emergencies, how decisions can be challenged, and how the settlement learns after harm or failure. Crises do not only test whether there is enough supply; they reveal whether the community's charter protects the vulnerable, excludes people from help, delays urgent action, overloads local systems, or adapts under pressure.

## 2. Core Fantasy

The player is the constitutional architect of a fragile settlement. They build and steward the community through visible needs like housing, food, work, infrastructure, and public trust, but the deeper fantasy is not direct control over every citizen or resource. It is the power to draft, test, and revise the civic machinery that decides how the settlement acts when pressure arrives.

Crises turn those institutions into lived consequences. A shortage, disaster, scandal, migration wave, or legitimacy shock should reveal whether the charter protects people, excludes them, slows urgent action, concentrates authority, overloads local systems, or creates room for adaptation. The player should feel that failure is not only a loss condition; it is evidence about what their institutions actually do.

The game should center the human story behind those systems. When the player inspects a citizen, household, or district, they should be able to see a life shaped by the charter: where that person lives, what they need, who depends on them, what happened to them during a crisis, and which institutional choice helped, harmed, delayed, or ignored them. Metrics explain the scale of a consequence; life stories explain its meaning.

## 3. Core Loop

```text
Build institutions -> face crises -> inspect consequences -> revise the system -> try again
```

**Build institutions**: The player selects or amends civic rules that define allocation, authority, oversight, appeals, local autonomy, and emergency powers. These choices should feel like drafting a charter, not just choosing passive modifiers.

**Face crises**: The settlement encounters a scarcity, shock, or legitimacy challenge that pushes those rules into action. Crises should be concrete enough to read as survival problems and systemic enough to test the institution behind the response.

**Inspect consequences**: The player reviews metrics, event timelines, harms, bottlenecks, district effects, and citizen reactions. The goal is to understand what happened, who benefited, who was left exposed, and which rule or dependency shaped the outcome.

Citizen inspection should make this personal without breaking the simulation boundary. A citizen story can include biography, household context, testimony, recent events, changing trust, and visible needs, but those details should be anchored to structured run outputs such as access, harm, aid, appeals, displacement, illness, recovery, or exclusion.

**Revise the system**: The player amends rules based on the inquiry rather than simply optimizing a build order. Revisions should answer a visible failure, tradeoff, or unintended consequence from the previous crisis.

**Try again**: The player continues the settlement, reruns a crisis, or compares a different institutional configuration under declared assumptions. Repetition is part of the fantasy: the settlement becomes a civic experiment whose failures can be studied and revised.

In the north-star game, "try again" can eventually include counterfactual timeline branching. The player may return to a prior crisis, inquiry, amendment window, or saved decision point, change the charter or surrounding conditions, and simulate forward to see what would have happened under a different institutional choice. This should be presented as assumption-bound comparison, not as proof that one real-world system is universally correct.

## 4. Five Pillars

1. **Visible settlement**
   The long-term game should make the settlement legible as a place under pressure. Citizens, districts, homes, services, resource flows, infrastructure, and ecological constraints should be visible enough that institutional outcomes feel grounded in lived conditions rather than abstract scores alone.

2. **Playable institutions**
   The player should draft and amend rules for allocation, authority, transparency, accountability, appeals, local autonomy, and emergency power. These rules are the central tools of play: they should be understandable as civic choices, mapped to simulation behavior, and exposed through player-facing language rather than hidden model switches.

3. **Crisis-driven simulation**
   Seasons, disasters, shortages, corruption events, migration, housing loss, public health needs, and supply breakdowns should test the system. A crisis should pressure both the settlement's material capacity and its civic design, revealing how the charter behaves when ordinary administration is not enough.

4. **Inspectable consequences**
   Outcomes should be shown through metrics, event timelines, citizen stories, district changes, public inquiries, and comparison reports. The player should be able to trace a result back to visible conditions, rules, bottlenecks, and tradeoffs, including who was protected, who was harmed, and where the institution failed to adapt. Human stories should be inspectable from the same evidence: a citizen's life story may dramatize an outcome, but it should not invent facts that the simulation did not produce.

5. **Revision over optimization**
   Failure should be informative. The game should reward learning, amendment, reruns, stress tests, comparison, and eventually counterfactual timeline branches rather than a single universal best answer. The player is not solving politics once; they are studying how a declared institutional design behaves under declared assumptions.

## 5. Scope Distinction

This brief describes the long-term product direction, not the full feature set of the first playable slice. The project should keep three related scopes distinct:

- **Constitutional Survival City Builder** is the north-star game: spatial settlement management plus institutional design. It includes the visible city, persistent citizens, districts, infrastructure, resources, crises, public inquiries, charter revision, and eventually counterfactual timeline branching.
- **Charterfall** is the first vertical slice: a compact roguelike settlement governance prototype that proves the institutional loop before full city-building systems are added. It can use abstract settlement state, cards, clauses, metrics, events, citizen story cards, reruns, and comparisons to test whether drafting institutions and inspecting consequences is fun.
- **PolityKit** is the reusable simulation framework underneath both. It should provide deterministic runs, scenarios, models, metrics, events, comparisons, stress tools, and structured outputs that the game layer can present, but it should not own Charterfall's fiction, progression, UI, campaign structure, citizen biographies, or commercial product identity.
