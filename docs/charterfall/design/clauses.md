# Charterfall Playable Institutional Clauses

This catalog defines the first player-facing charter surface for Charterfall. It is design documentation, not a machine-readable schema yet.

Milestone 1 can start with one selected model or preset per run, then grow toward composed charters. Until a clause-composition contract exists, options marked `game-layer-only` can appear in the UI, tutorial, inquiry copy, and campaign state, but should not be sent as simulation effects.

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
