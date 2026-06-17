# PolityKit Game Roadmap

This roadmap describes a game direction built on top of PolityKit: a constitutional survival city builder with a smaller vertical slice called Charterfall.

The central design loop is:

```text
Build institutions -> face crises -> inspect consequences -> revise the system -> try again
```

PolityKit remains the deterministic governance simulation layer. The game adds player-facing systems, fiction, visual feedback, progression, city state, and scenario design on top.

## Product Vision

Create a game where institutions are playable systems, not background flavor.

Players should not only build farms, wells, clinics, and shelters. They should design the rules by which a settlement allocates scarce resources, handles emergencies, resolves appeals, reveals information, manages corruption risk, and adapts after failure.

The game should make institutional tradeoffs legible through play:

- A rule can improve survival while reducing trust.
- A transparent process can increase legitimacy while increasing administrative load.
- Emergency powers can prevent immediate collapse while creating future abuse risk.
- Local autonomy can improve responsiveness while making coordination harder.
- Market-like allocation can move resources quickly while leaving vulnerable citizens exposed.
- Participatory systems can improve fairness while struggling under time pressure.

The game should avoid claiming that one real-world system is universally best. It should frame every outcome as the result of declared assumptions, scenario conditions, and model behavior.

## North Star Game: Constitutional Survival City Builder

The long-term game is a settlement survival and city-building game where governance rules shape how the settlement behaves under stress.

### Core Fantasy

You are not simply the mayor. You are the founder, steward, or constitutional architect of a fragile settlement. You can build infrastructure, but your deeper power is designing the civic machinery that decides who gets help, who gets heard, and how the community responds when scarcity arrives.

### Core Pillars

1. **Visible settlement**
   The player manages districts, resources, infrastructure, citizens, services, and ecological constraints.

2. **Playable institutions**
   The player drafts and amends rules for allocation, authority, transparency, accountability, appeals, local autonomy, and emergency power.

3. **Crisis-driven simulation**
   Seasons, disasters, shortages, corruption events, migration, housing loss, public health needs, and supply breakdowns test the system.

4. **Inspectable consequences**
   Outcomes are presented through metrics, event timelines, citizen stories, district changes, public inquiries, and comparison reports. Citizen inspection should connect personal life stories to the rules, events, and bottlenecks that shaped them.

5. **Revision over optimization**
   Failure is part of play. The player learns by changing assumptions, rerunning from the same seed, stress-testing variants, branching from earlier decision points, and comparing outcomes.

### PolityKit Role

PolityKit should provide:

- Deterministic simulation runs.
- Shared scenarios and seeds.
- Governance models and presets.
- Metrics such as needs met, inequality, trust, severe failures, and administrative load.
- Event streams for explaining what happened.
- Structured citizen, household, or group outcomes when the game needs personal story surfaces.
- Reruns from stored configurations.
- Comparisons between runs.
- Parameter sweeps for tuning.
- Stress sweeps for robustness and collapse analysis.
- Advisory AI summaries or scenario suggestions only when explicitly enabled.

The game layer should provide:

- City map and spatial state.
- Citizen and faction presentation.
- Citizen story cards and inspectable life histories.
- Player controls and UI.
- Narrative events.
- Progression and unlocks.
- Visual feedback.
- Save games.
- Scenario campaign structure.
- Counterfactual timeline branching from stored decision points.
- Translation between game rules and PolityKit model/scenario inputs.

## Vertical Slice: Charterfall

Charterfall is the first playable vertical slice. It proves the institutional game loop before the project commits to a full city builder.

### Pitch

Charterfall is a roguelike settlement game where each run asks the player to draft a civic charter, survive a chain of crises, inspect the consequences, and amend the system before the next crisis.

It uses an abstracted settlement instead of a full spatial city simulation. The interface can focus on cards, clauses, metrics, events, and citizen/faction reactions.

### Why Start Here

Charterfall is smaller than the full city builder but still tests the most important risk: whether drafting institutions and watching their consequences is fun.

It can reuse PolityKit's current strengths:

- `POST /api/runs` for crisis resolution.
- `GET /api/runs/{id}/dashboard` for summaries, metrics, and events.
- `POST /api/runs/{id}/rerun` for amendments from the same starting conditions.
- `GET /api/runs/{id}/compare/{comparisonId}` for before/after consequences.
- `POST /api/runs/sweep` for showing sensitivity.
- `POST /api/runs/stress` for testing robustness across scenarios and seeds.

### Charterfall Core Loop

1. **Draft**
   Choose a starting charter from a small set of institutional clauses.

2. **Forecast**
   Preview known risks, available resources, citizen needs, and faction pressures.

3. **Crisis**
   Resolve a scenario through PolityKit.

4. **Inquiry**
   Review what happened: metrics, events, harmed groups, bottlenecks, collapse signals, and surprising effects.

5. **Amend**
   Change one or more clauses or parameters.

6. **Compare**
   Rerun or compare against the prior outcome.

7. **Advance**
   Carry consequences into the next crisis, unlock new clauses, and face harder scenario combinations.

### Minimum Playable Scope

The first slice should include:

- One settlement profile.
- Three crisis scenarios.
- Three to five charter dimensions.
- Three baseline governance presets plus a small set of player-buildable clauses.
- Five displayed metrics.
- Event timeline with filtering.
- Same-seed rerun.
- Before/after comparison.
- A simple campaign of three crises.
- A fail state based on collapse, severe unmet need, or trust breakdown.
- A win state based on surviving the crisis chain while meeting minimum civic thresholds.

### Example Charter Dimensions

- Allocation method: need-based, market-based, hierarchy-based, participatory, hybrid.
- Decision authority: council, expert office, local districts, emergency executive.
- Transparency: public ledger, delayed reporting, closed administration.
- Accountability: audit office, appeal board, citizen review, none.
- Emergency powers: none, limited, renewable, broad.
- Property and access norms: common pool, vouchers, private exchange, ration entitlement.
- Information flow: open reports, representative reports, centralized reporting.

### Example Crises

- Food shortage after a failed harvest.
- Medicine shortage during disease spread.
- Housing displacement after storm damage.
- Corruption pressure in a supply office.
- Refugee arrival during existing scarcity.
- Infrastructure failure across unequal districts.

## Milestones

### Milestone 0: Product Framing

Goal: turn the concept into a buildable design target.

Deliverables:

- Game design brief for Constitutional Survival City Builder.
- Charterfall one-page pitch.
- Core loop diagram.
- Definition of playable institutional clauses.
- Mapping from game clauses to PolityKit models, parameters, and scenarios.
- Design boundaries for political framing and claims.

Acceptance:

- A new contributor can explain the game loop in one minute.
- Every player-facing rule maps to a simulation input or a deliberate game-layer-only effect.
- The project states clearly that outcomes are assumption-bound, not real-world proof.

### Milestone 1: Charterfall Prototype

Goal: create a playable text-and-dashboard prototype.

Deliverables:

- Lightweight web app or desktop UI.
- Scenario selection.
- Charter clause selection.
- API integration for run creation.
- Dashboard view for metrics and events.
- Rerun with amended clauses.
- Comparison view.
- Three-crisis campaign script.

Acceptance:

- A player can complete a run from draft to final outcome.
- A player can amend a charter and compare the amended run against the original.
- Outcomes are understandable without reading raw JSON.

### Milestone 2: Game Feel And Readability

Goal: make the abstract simulation feel like a game.

Deliverables:

- Citizen and faction reaction snippets.
- Citizen story cards grounded in run outputs.
- Inspectable household or citizen summaries for selected crisis outcomes.
- Crisis cards.
- Clause cards with clear tradeoffs.
- Public inquiry screen after each crisis.
- Run history.
- Outcome badges for collapse, recovery, fairness, trust, and administrative burden.
- Tutorialized first run.

Acceptance:

- A new player understands why a run succeeded or failed.
- A player can inspect at least one citizen or household story and trace it back to simulation events.
- The UI invites experimentation rather than punishing failure.
- The player can distinguish metrics, events, and narrative interpretation.

### Milestone 3: Systems Depth

Goal: make repeated play interesting.

Deliverables:

- Clause synergies and tensions.
- Unlockable institutional mechanisms.
- Multi-crisis consequences carried between chapters.
- Faction pressure and legitimacy effects.
- Counterfactual branch design for restarting from selected inquiry or amendment points.
- Stress test mode.
- Sensitivity report view.
- Challenge scenarios with scoring constraints.

Acceptance:

- Different charter builds produce meaningfully different outcomes.
- The same charter can perform well in one crisis and poorly in another.
- Counterfactual branches are scoped to clear decision points rather than arbitrary save-state rewinds.
- Stress and sensitivity outputs create useful player decisions, not just reports.

### Milestone 4: City Builder Foundation

Goal: begin expanding from Charterfall into the larger settlement game.

Deliverables:

- District model.
- Spatial resource sources and service access.
- Basic construction and maintenance.
- Population groups attached to districts.
- District-level needs and trust.
- Translation layer from city state to PolityKit scenario inputs.
- Visual settlement state that changes after each crisis.

Acceptance:

- The city layer changes simulation inputs in understandable ways.
- PolityKit outcomes change visible district conditions.
- The game remains playable when spatial state is added.

### Milestone 5: Constitutional Survival City Builder Alpha

Goal: combine city management and institutional design into one coherent game.

Deliverables:

- Full season loop.
- Buildable infrastructure.
- Charter amendment windows.
- District events.
- Faction demands.
- Resource production and storage.
- Crisis scheduling.
- Public inquiry and comparison tools.
- Save/load.

Acceptance:

- The player can build, govern, survive, inspect, and revise across multiple seasons.
- City management and governance design both matter.
- The simulation remains deterministic and replayable for the same seed and configuration.

## Future Extensions

### Counterfactual Timeline Branching

Let players return to a prior point in a completed or ongoing timeline, change a charter clause, amendment, or declared scenario condition, and simulate forward to see what would have happened. Branches should compare against the original timeline through metrics, event timelines, harms, bottlenecks, citizen reactions, and public inquiry summaries.

Value:

- Turns failure into an inspectable design space.
- Makes "what if we had governed differently?" a first-class play action.
- Supports same-seed experimentation beyond restarting from the initial scenario.
- Gives the long-term city builder a distinctive institutional time-study fantasy.

Risks:

- Can balloon into full arbitrary save-state rewind if not constrained.
- Needs clear UI for timeline ancestry, branch comparison, and player intent.
- Requires careful language so counterfactual outcomes remain assumption-bound.

Initial scope:

- Not required for Milestone 1.
- Prefer branching at crisis boundaries, inquiry screens, amendment windows, or authored checkpoint events.
- Avoid promising tick-by-tick rewind until save-state, campaign state, and comparison tooling are mature.

### Narrative Political RPG Layer

Add named advisors, faction leaders, organizers, administrators, and citizens. PolityKit outcomes drive character reactions, trust, demands, resignations, protests, and alliances.

Value:

- Makes systemic outcomes emotionally legible.
- Turns abstract tradeoffs into memorable stories.
- Supports campaign writing and replayable event chains.

Boundary:

- Personal stories should stay grounded in deterministic outcomes, even when presentation text is authored or assisted.

### Multiplayer Ideology Lab

Let players submit charters to shared scenario packs and compare outcomes under the same assumptions.

Value:

- Encourages constructive disagreement.
- Makes assumptions inspectable.
- Supports community challenges and tournaments.

Risks:

- Moderation burden.
- Bad-faith naming or framing.
- Scoring disputes.
- Need for careful language around real-world ideology.

### Institution Auto-Battler Mode

Turn charter clauses into draftable components. Each round, players draft institutions, face crisis matchups, and optimize for multi-objective outcomes.

Value:

- Strong replayability.
- Streamable runs.
- Faster session length than the city builder.

Risks:

- May flatten institutional nuance into card stats.
- Needs excellent UI clarity.

### Scenario Architect

Expose scenario creation tools for players and educators.

Value:

- Extends replayability.
- Enables classroom and workshop use.
- Builds community content around explicit assumptions.

Needs:

- Scenario validation.
- Shareable scenario packs.
- Safety and wording guidelines.
- Clear separation between fictional scenarios and real-world claims.

### Living World NPC Government

Use PolityKit to simulate background governments in a larger RPG, colony sim, or strategy sandbox.

Value:

- Towns, guilds, districts, and factions can govern themselves while the player acts elsewhere.
- Creates systemic world changes without hand-authoring every outcome.

Potential product path:

- Internal technology for the city builder.
- Later reusable module or SDK.

### Historical Pattern Scenario Packs

Create historically inspired but abstract scenario packs around famine, industrialization, post-disaster rebuilding, migration, frontier settlement, or institutional corruption.

Value:

- Educational appeal.
- Rich scenario variety.
- Good fit for public workshops.

Boundary:

- Avoid claiming to simulate real history directly.
- Keep assumptions inspectable and editable.

## Design Guardrails

- Do not present simulation outputs as proof of real-world superiority.
- Make assumptions visible.
- Keep deterministic simulation data separate from advisory text.
- Treat AI-generated content, if used, as optional and non-authoritative.
- Avoid caricatured labels for real communities or ideologies.
- Prefer fictional settlements and abstract scenario packs for the core game.
- Let players compare tradeoffs across metrics instead of chasing a single universal score.
- Make failure informative and recoverable.

## Technical Direction

### Integration Shape

The first Charterfall implementation can use PolityKit as an API-backed simulation service.

Likely calls:

- `GET /api/models`
- `GET /api/metrics`
- `GET /api/scenarios`
- `POST /api/runs`
- `GET /api/runs/{id}/dashboard`
- `POST /api/runs/{id}/rerun`
- `GET /api/runs/{id}/compare/{comparisonId}`
- `POST /api/runs/sweep`
- `POST /api/runs/stress`

### New Game-Layer Concepts

The game will likely need its own project or app layer for:

- Player profile and campaign progress.
- Charter definitions.
- Clause unlocks.
- Timeline branch metadata and comparison history.
- Game-specific scenario packs.
- Presentation text.
- Narrative events.
- Citizen biographies, testimony, and life-history presentation.
- City/district state.
- Save files.
- UI state.

### PolityKit Extensions To Consider

- First-class charter or institution composition contracts.
- Scenario tags and difficulty metadata.
- More granular citizen group and district outputs.
- Structured citizen or household case files for story cards.
- Collapse and recovery explanations formatted for gameplay.
- Stable event categories for UI filtering.
- Run annotations for game campaign context.
- Checkpoint or branch contracts for rerunning from declared decision points.
- Batch comparison endpoints tuned for challenge screens.

## Open Questions

- Should Charterfall be a separate app in this repository or a separate repository consuming PolityKit?
- Should the first UI be web-based, desktop, or terminal-first?
- How much city state should exist before PolityKit receives a scenario?
- Which metrics are player-facing by default, and which remain advanced diagnostics?
- How should charter clauses be balanced: authored by hand, parameterized, or generated from governance dimensions?
- Should scoring be threshold-based, multi-objective, or narrative-only?
- How much randomness belongs in the game layer if PolityKit runs are seed-deterministic?

## Recommended Next Step

Build the Charterfall prototype before building the full city builder.

The prototype should answer one question:

> Is it fun to draft institutions, watch them fail or adapt under crisis, and revise them based on inspectable consequences?

If the answer is yes, expand outward into districts, construction, factions, narrative campaigns, and the full Constitutional Survival City Builder.
