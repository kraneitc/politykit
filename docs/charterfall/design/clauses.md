# Charterfall Playable Institutional Clauses

This catalog defines the first player-facing charter surface for Charterfall. It is design documentation, not a machine-readable schema yet.

Milestone 1 can start with one selected model or preset per run, then grow toward composed charters. Until a clause-composition contract exists, options marked `game-layer-only` can appear in the UI, tutorial, inquiry copy, and campaign state, but should not be sent as simulation effects.

Several clauses should also create power incentives: tempting shortcuts that help the player survive a crisis while raising later civic risk. These incentives should be framed through concrete pressures such as precedent, opacity, capture, accountability debt, and legitimacy strain, not through a flat good/evil score.

## Prototype Dimensions

| Dimension | First playable options | Player-facing question |
|---|---|---|
| Allocation method | Need-based, market-based, hierarchy-based, participatory, hybrid | Who gets scarce resources first? |
| Decision authority | Council, expert office, local districts, emergency executive | Who can make binding crisis decisions? |
| Transparency | Public ledger, delayed reporting, closed administration | How visible are decisions and shortages? |
| Accountability | Appeal board, audit office, citizen review, none | Who can challenge or review decisions? |
| Emergency powers | None, limited, renewable, broad | What can be bypassed during acute danger? |

## Mapping Status

| Status | Meaning |
|---|---|
| `run-model` | Can directly select an existing PolityKit model or preset for `POST /api/runs`. |
| `preset-dimension` | Maps to an existing composite governance preset dimension value, but needs a later composition contract before individual clauses can be mixed freely. |
| `game-layer-only` | Safe for UI, fiction, unlocks, tutorial copy, and campaign state, but not authoritative simulation input yet. |

## Clause Catalog

| Stable ID | Dimension | Display name | Description | Tradeoff | PolityKit mapping | Game-layer effects | Availability | Boundary |
|---|---|---|---|---|---|---|---|---|
| `allocation.need_based` | Allocation method | Need-Based Allocation | Prioritize citizens with the highest unmet need. | Can protect vulnerable citizens, but may increase administrative load and depend on accurate assessment. | `run-model`: `need-based-allocation`; parameters: `needPriorityWeight=1.0`, `vulnerabilityPriorityWeight=0.5`. | Highlights unmet need and vulnerability in inquiry copy. | First run. | Represents a simplified crisis rule, not a claim about any real institution. |
| `allocation.market_based` | Allocation method | Market-Based Allocation | Let price-like scarcity signals and ability to pay shape access. | Can move resources with low administrative burden, but may leave vulnerable citizens exposed. | `run-model`: `market-based-allocation`. | Highlights exclusion risk and wealth-mediated access in inquiry copy. | First run. | Represents a fictional market-like crisis rule under declared assumptions. |
| `allocation.hierarchy_based` | Allocation method | Hierarchy-Based Allocation | Give priority to rank, office, or command position. | Can be fast and legible in emergencies, but may normalize unequal protection. | `run-model`: `hierarchy-based-allocation`. | Highlights rank, patronage, and ignored low-power groups in inquiry copy. | First run. | Represents a fictional hierarchy rule under declared assumptions. |
| `allocation.participatory` | Allocation method | Participatory Allocation | Let residents deliberate or submit local knowledge before scarce resources are assigned. | Can surface hidden needs, but may slow decisions under time pressure. | `run-model`: `CompositeGovernance:participatory-commons`; also maps to `AllocationMechanism=need-weighted-rationing`. | Unlocks assembly-flavored clause wording and public inquiry testimony. | First run as preset option; composed clause later. | Represents a fictional participatory mechanism, not a claim that all participatory systems behave this way. |
| `allocation.hybrid` | Allocation method | Hybrid Allocation | Combine need signals, rule-based administration, and limited exchange. | Can balance multiple pressures, but may be harder for players and citizens to understand. | `run-model`: `CompositeGovernance:regulated-market` or `CompositeGovernance:technocratic-administration` depending on authored scenario. | Marks the charter as mixed and uses comparison copy that calls out competing criteria. | First run if preset mode is used; otherwise deferred. | Represents an authored blend inside the model assumptions. |
| `authority.council` | Decision authority | Council Authority | A small civic council can make binding crisis decisions. | Can balance viewpoints, but may be slower than a single office. | `preset-dimension`: `DecisionAuthority=participatory-assembly` or later council-specific value. | Council portrait, vote, and faction reaction copy. | First run as UI clause. | Fictional authority structure only. |
| `authority.expert_office` | Decision authority | Expert Office | A professional office can issue crisis directives using indicators and reports. | Can act consistently, but may miss lived vulnerability outside the metrics. | `preset-dimension`: `DecisionAuthority=bureau-technocratic-administration`. | Expert briefing copy and indicator-focused inquiry framing. | First run as UI clause. | Fictional administrative structure only. |
| `authority.local_districts` | Decision authority | Local Districts | District groups can make or adapt decisions close to affected citizens. | Can respond to local conditions, but coordination can become uneven. | `preset-dimension`: `DecisionAuthority=local-federated-delegation`. | District-focused story cards and local autonomy copy. | First run as UI clause. | Fictional local autonomy structure only. |
| `authority.emergency_executive` | Decision authority | Emergency Executive | A single emergency office can bypass ordinary deliberation during acute danger. | Can move quickly, but concentrates power and can reduce trust. | `game-layer-only` until emergency-power parameters exist. | Unlocks emergency action copy and future abuse-risk badges. | Deferred simulation effect. | Fictional emergency rule, not real-world endorsement. |
| `transparency.public_ledger` | Transparency | Public Ledger | Allocation decisions and shortages are visible to the settlement. | Can build trust and reveal failure, but increases administrative work and political exposure. | `preset-dimension`: `InformationFlow=transparent-local-feedback` or `transparent-indicator-reporting`. | Shows public ledger language in inquiry and comparison screens. | First run as UI clause. | Fictional transparency rule under declared assumptions. |
| `transparency.delayed_reporting` | Transparency | Delayed Reporting | Reports are published after crisis actions are complete. | Can reduce immediate burden, but delays correction and public understanding. | `game-layer-only` until delayed-reporting effects exist. | Delays some explanatory copy until inquiry phase. | First run as UI clause. | Fictional reporting rule only. |
| `transparency.closed_administration` | Transparency | Closed Administration | Officials keep allocation information internal during the crisis. | Can reduce friction, but may hide harms and lower trust. | `preset-dimension`: `InformationFlow=opaque-restricted-information`. | Adds secrecy and rumor language to citizen reactions. | First run as UI clause. | Fictional information rule, not a real-world proof claim. |
| `accountability.appeal_board` | Accountability | Appeal Board | Citizens can challenge allocation decisions through a formal appeal. | Can correct errors, but may be too slow for urgent needs. | `preset-dimension`: `AppealProcess=formal-community-review`, `formal-regulatory-appeal`, or `formal-administrative-review`. | Adds appeal events and testimony prompts when supported by run outputs. | First run as UI clause. | Fictional appeal process under declared assumptions. |
| `accountability.audit_office` | Accountability | Audit Office | An office reviews decisions and records for visible failures. | Can reveal systemic problems, but may add administrative load. | `preset-dimension`: `AccountabilityMechanism=oversight-audit` or `state-audit`. | Adds audit language to inquiry and outcome badges. | First run as UI clause. | Fictional audit process only. |
| `accountability.citizen_review` | Accountability | Citizen Review | Residents can review decision makers or demand recall after harm. | Can improve legitimacy, but may increase conflict and process time. | `preset-dimension`: `AccountabilityMechanism=recall-and-audit` or `peer-recall-and-audit`. | Adds citizen testimony and review-screen framing. | First run as UI clause. | Fictional civic review process only. |
| `accountability.none` | Accountability | No Formal Review | There is no formal path to challenge crisis decisions. | Can reduce process cost, but leaves errors and exclusion harder to correct. | `preset-dimension`: closest existing value is `weak-informal-accountability`; direct none value deferred. | Suppresses appeal-board UI and highlights unreviewed harms in inquiry copy. | First run as UI clause. | Fictional absence of formal review only. |
| `emergency.none` | Emergency powers | No Emergency Powers | Ordinary rules remain in force during acute danger. | Can protect process and trust, but may delay urgent action. | `game-layer-only` until emergency-power model inputs exist. | Restricts emergency-action copy and unlocks process-protection reactions. | First run as UI clause. | Fictional crisis rule only. |
| `emergency.limited` | Emergency powers | Limited Emergency Powers | A narrow set of procedures can be bypassed for one crisis response. | Can address urgent danger, but may create accountability pressure. | `game-layer-only` until emergency-power model inputs exist. | Enables one authored emergency beat or badge. | First run as UI clause. | Fictional crisis rule only. |
| `emergency.renewable` | Emergency powers | Renewable Emergency Powers | Emergency powers can be extended after review. | Can preserve flexibility, but risks becoming normal policy. | `game-layer-only` until emergency-power model inputs exist. | Adds renewal decision copy between crises. | Deferred for campaign structure. | Fictional crisis rule only. |
| `emergency.broad` | Emergency powers | Broad Emergency Powers | Crisis leaders can bypass most ordinary checks during acute danger. | Can move quickly, but risks abuse, exclusion, and legitimacy loss. | `game-layer-only` until emergency-power model inputs exist. | Adds concentrated-power reactions and future abuse-risk copy. | Deferred simulation effect. | Fictional crisis rule, not endorsement. |

## Clause-To-PolityKit Mapping

This table maps the first Charterfall clause surface to existing PolityKit surfaces. A prototype developer should be able to construct a `POST /api/runs` request from any `run-model` entry. `preset-dimension` and `game-layer-only` entries remain player-facing design surfaces until a composition contract exists.

| Game concept | Charterfall clause IDs | PolityKit surface | Prototype use | Notes |
|---|---|---|---|---|
| Need-based allocation | `allocation.need_based` | `need-based-allocation` model | Set `models: ["need-based-allocation"]`. | Existing baseline model. Default parameters: `needPriorityWeight=1.0`, `vulnerabilityPriorityWeight=0.5`. |
| Market-based allocation | `allocation.market_based` | `market-based-allocation` model | Set `models: ["market-based-allocation"]`. | Existing baseline model. Use for fast, low-administration allocation comparisons. |
| Hierarchy-based allocation | `allocation.hierarchy_based` | `hierarchy-based-allocation` model | Set `models: ["hierarchy-based-allocation"]`. | Existing baseline model. Use for rank- or authority-weighted access comparisons. |
| Composite governance bundle | Preset-backed clause groups | `CompositeGovernance:<preset-id>` or preset alias | Set `models: ["participatory-commons"]`, `["regulated-market"]`, or `["technocratic-administration"]`; API resolves to composite models. | Use current preset manifests as source of truth. Do not freely mix dimensions until composition exists. |
| Participatory commons | `allocation.participatory`, `authority.council`, `transparency.public_ledger`, `accountability.citizen_review`, `accountability.appeal_board` | `CompositeGovernance:participatory-commons` | Candidate first charter preset or comparison model. | Maps to participatory assembly, transparent feedback, recall/audit, community review, and need-weighted rationing. |
| Regulated market | `allocation.hybrid`, `allocation.market_based`, `transparency.public_ledger`, `accountability.audit_office`, `accountability.appeal_board` | `CompositeGovernance:regulated-market` | Candidate first charter preset or comparison model. | Maps to market-price weighting, public reporting, oversight audit, and formal regulatory appeal. |
| Technocratic administration | `allocation.hybrid`, `authority.expert_office`, `transparency.public_ledger`, `accountability.audit_office`, `accountability.appeal_board` | `CompositeGovernance:technocratic-administration` | Candidate first charter preset or comparison model. | Maps to need-and-risk scoring, professional administration, transparent indicators, oversight audit, and formal review. |
| Council authority | `authority.council` | Preset dimension value such as `DecisionAuthority=participatory-assembly` | Presentation-only unless a preset is selected. | Use as player-facing explanation for participatory presets until custom profile composition exists. |
| Expert office | `authority.expert_office` | `DecisionAuthority=bureau-technocratic-administration` | Presentation-only unless a preset is selected. | Use as player-facing explanation for technocratic presets. |
| Local districts | `authority.local_districts` | `DecisionAuthority=local-federated-delegation` | Presentation-only unless `mutual-aid-federation` or a future district preset is selected. | Candidate later preset; not part of the recommended first three preset options. |
| Emergency executive | `authority.emergency_executive` | No direct model input yet. | `game-layer-only`. | Can drive copy, badges, or power-incentive hooks; do not send as a model parameter yet. |
| Public ledger | `transparency.public_ledger` | `InformationFlow=transparent-local-feedback` or `transparent-indicator-reporting` | Presentation-only unless a preset is selected. | Use to explain transparency-heavy presets and inquiry UI. |
| Delayed reporting | `transparency.delayed_reporting` | No direct model input yet. | `game-layer-only`. | Can drive opacity copy or delayed inquiry reveal; do not alter deterministic results yet. |
| Closed administration | `transparency.closed_administration` | `InformationFlow=opaque-restricted-information` | Presentation-only unless `patronage-hierarchy` or a future closed preset is selected. | Candidate later preset; useful for opacity and trust tradeoffs. |
| Appeal board | `accountability.appeal_board` | `AppealProcess=formal-community-review`, `formal-regulatory-appeal`, or `formal-administrative-review` | Presentation-only unless a preset is selected. | Choose the exact appeal value from the selected preset manifest. |
| Audit office | `accountability.audit_office` | `AccountabilityMechanism=oversight-audit` or `state-audit` | Presentation-only unless a preset is selected. | Use current preset manifests for exact value and administrative-load effects. |
| Citizen review | `accountability.citizen_review` | `AccountabilityMechanism=recall-and-audit` or `peer-recall-and-audit` | Presentation-only unless a preset is selected. | Good fit for participatory or mutual-aid presets. |
| No formal review | `accountability.none` | Closest existing value: `AccountabilityMechanism=weak-informal-accountability`; direct none value deferred. | Presentation-only unless a suitable preset is selected. | Keep wording clear that no direct `none` model value exists yet. |
| Emergency powers | `emergency.none`, `emergency.limited`, `emergency.renewable`, `emergency.broad` | No direct model input yet. | `game-layer-only`. | Candidate future model parameter, scenario modifier, or campaign carryover rule. |
| Food crisis | Scenario selection | `village-food-crisis` or `examples/village-food-crisis.json` | Set `scenario: "village-food-crisis"` for first prototype runs. | Existing built-in scenario and example file. |
| Medicine crisis | Scenario selection | `examples/medicine-shortage.json` | Scenario authoring/selection surface. | Present in example stress docs; use if suitable for Charterfall content pack. |
| Corruption pressure | Scenario selection | `examples/corruption-stress.json` | Scenario authoring/selection surface. | Good first pressure test for transparency, accountability, and power incentives. |
| Same-seed amendment | Run operation | `POST /api/runs/{id}/rerun` | Use after the player changes clauses following inquiry. | Rerun should preserve starting conditions unless the player deliberately changes seed/scenario. |
| Before/after view | Run operation | `GET /api/runs/{id}/compare/{comparisonId}` | Use after rerun or branch comparison. | Present deltas as "changed under this run," not proof of superiority. |
| Public inquiry dashboard | Run operation | `GET /api/runs/{id}/dashboard` | Source for summary metrics, events, and event timeline. | Game layer turns deterministic outputs into inquiry UI and citizen story cards. |
| Robustness preview | Run operation | `POST /api/runs/stress` | Milestone 3+ by default. | Can become advanced prototype mode after the core loop is stable. |

### Run Construction Rules

- Prefer existing model IDs and governance preset IDs for Milestone 1.
- Do not invent UI clauses that imply simulation effects before the mapping exists.
- If a clause is presentation-only, mark it as `game-layer-only` and keep it out of the PolityKit run configuration.
- If a clause maps to a parameter, record the exact parameter name, default value, allowed range, and why the range is game-safe before exposing player tuning.
- If multiple clauses affect the same PolityKit parameter, define precedence before implementation.
- If a player selects a baseline allocation clause plus additional UI-only clauses, send only the baseline model and approved parameters to PolityKit; persist the other clauses in game-layer campaign state.
- If a player selects a preset-backed charter, send the preset ID or `CompositeGovernance:<preset-id>` model to PolityKit and display the preset's dimensions as explanatory clauses.

### Prototype Request Examples

Baseline allocation run:

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

Preset-backed charter run:

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

## Power-Incentive Hooks

| Incentive pressure | Example triggers | Short-term player benefit | Long-term risk to surface |
|---|---|---|---|
| Precedent | `emergency.limited`, `emergency.renewable`, `emergency.broad`, `authority.emergency_executive` | Faster action, fewer blocked decisions, simpler crisis resolution. | Exceptions become normal, future bypasses face less resistance, accountability weakens. |
| Opacity | `transparency.delayed_reporting`, `transparency.closed_administration` | Lower immediate panic, lower reporting burden, cleaner short-term public inquiry. | Hidden harms, rumor, weaker diagnosis, delayed correction. |
| Capture | `allocation.hierarchy_based`, faction-favored scenario choices, future patronage clauses | Access to resources, stability, labor, or political support. | Powerful groups become harder to refuse and low-power citizens become easier to ignore. |
| Accountability debt | `accountability.none`, narrowed appeal access, ignored audit findings | Faster decisions and lower administrative load. | Unresolved harms accumulate and later legitimacy costs become sharper. |
| Metric gaming | Any clause combination optimized only for visible thresholds | Easier wins against displayed metrics. | Severe individual stories, hidden district harms, or brittle institutions emerge later. |

Milestone 1 can expose these hooks through inquiry copy and outcome badges before they become full simulation state. For example, using broad emergency power can add a "precedent set" inquiry note even if the deterministic run only reflects the selected model or preset.

## First Prototype Guidance

For the earliest playable run, prefer one of these implementation paths:

1. **Baseline model path**
   Let allocation clauses choose `need-based-allocation`, `market-based-allocation`, or `hierarchy-based-allocation`. Other clauses are recorded in the game layer and used for UI copy only.

2. **Preset path**
   Let the player choose an authored charter preset such as `participatory-commons`, `regulated-market`, or `technocratic-administration`. Show the underlying clauses as explanatory surfaces, but do not let players freely mix dimensions until composition exists.

3. **Hybrid path**
   Start with baseline allocation, then use one or two non-allocation clauses as game-layer-only story and inquiry modifiers. Mark them clearly as not yet changing deterministic simulation results.

Do not send game-layer-only clauses to PolityKit as if they were authoritative model parameters.

## Example Clause Shape

```json
{
  "id": "allocation.need_based",
  "dimension": "allocation_method",
  "displayName": "Need-Based Allocation",
  "description": "Prioritize citizens with the highest unmet need.",
  "tradeoff": "Can protect vulnerable citizens, but may increase administrative load.",
  "polityKit": {
    "status": "run-model",
    "model": "need-based-allocation",
    "parameters": {
      "needPriorityWeight": 1.0,
      "vulnerabilityPriorityWeight": 0.5
    }
  },
  "gameLayerEffects": [
    "Highlight unmet need and vulnerability in inquiry copy."
  ],
  "availability": "first-run",
  "boundary": "Represents a simplified crisis rule, not a claim about any real institution."
}
```

## Open Composition Questions

- Should Milestone 1 expose individual clauses, authored presets, or both?
- Which non-allocation clauses should become simulation inputs first?
- Should emergency powers be a model parameter, a scenario modifier, or a game-layer campaign rule?
- How should multiple clauses that affect administrative load compose without hiding tradeoffs from the player?
- Which power-incentive pressures should be visible in Milestone 1, and which should remain implied until campaign carryover exists?
