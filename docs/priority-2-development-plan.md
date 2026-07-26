# Priority 2 Development Plan

**Built from:** the 2026-07-25 engineering retrospective of the full codebase. **Extends:** [priority-1-development-plan.md](priority-1-development-plan.md) (Phases 1–4 complete as of this writing; Phases 5–8 defined there, refined here). **Adopted:** 2026-07-25 — this is the standing reference for step-by-step work after this point, tracked in [CLAUDE.md](../CLAUDE.md) the same way Priority 1 is.

This is a goal-oriented scaffolding across three horizons: short-term (finish what Priority 1 already scoped, plus decide what Priority 2 needs decided), medium-term (de-center Watchtower — turn the engine from "one scenario's orchestration code" into something actually loadable with any scenario), and long-term (become a game, not just an engine).

Where a step below refines a branch sequence already listed in `priority-1-development-plan.md`'s "Recommended Branch Sequence" section, **this document's sequence is authoritative going forward** for that phase — treat the original list as superseded for Phases 5 and 6 specifically, unchanged everywhere else.

## On "de-centering Watchtower"

This is exactly Phase 6 of the Priority 1 plan, "Scenario Content Boundary," and it belongs at the center of the medium-term horizon here. One clarification worth having settled before it starts: the original plan's wording ("move Watchtower constants into a definition factory") undersells the depth needed. The retrospective found that Watchtower isn't just *referenced* by otherwise-generic code — the generic code's own types are Watchtower-shaped:

- `ApplicationSessionState.Scenario` is typed directly as `WatchtowerScenarioState`, not a generic `ScenarioState`.
- `ApplicationSessionRules` hardcodes "exactly one Fighter, one Barbarian, one Ranger, exactly 3 members" as a universal session invariant, not scenario-supplied data.
- Public methods that should be generic verbs are named after the scenario: `ExplorationRules.CanEnterWatchtower`, `RegionalTravelRules.BeginWatchtowerJourney`.

So Phase 6 needs to make the *session state itself* scenario-parametric, not just extract content tables. That's Phase 6, Step 1 below, because everything else in the phase depends on it.

---

## Short-term (next 1–2 weeks): Stabilize, decompose, decide

**Goal:** finish the engineering foundation that's already scoped, clear cheap debt, and lock in the scope decisions Phase 6 needs answered before it starts.

### Priority 0 — Housekeeping sweep (~1 day, zero risk)

1. Delete 33 fully-merged stale branches (11 local, 22 remote)
2. Prune `docs/` scratch — 13 files / ~830 KB of superseded Phase 6 review artifacts from the earlier multi-agent review workflow
3. Add root `README.md`
4. Decide and add a `LICENSE`
5. Add a Debug-configuration CI job matching the documented local gate
6. Wire up `coverlet.collector` coverage reporting in CI, or drop the dependency
7. ~~Add a `dotnet format --verify-no-changes` CI step~~ — **rejected after evaluation (PR #75).** `dotnet format` disagrees with this codebase's deliberate hand-wrapped declaration style in every mode: it pushes `{ get; init; }` onto its own line across 11 files and puts switch-arm commas on their own lines in tests. Tuning `csharp_new_line_before_open_brace` only trades that for K&R braces on block-bodied properties, inconsistent with the Allman style used everywhere else. `dotnet format analyzers` already passes clean, and `EnforceCodeStyleInBuild` + `TreatWarningsAsErrors` cover real style violations at build time. The one genuine violation it surfaced (a missing final newline) was fixed directly.
8. Add dependency-vulnerability scanning (Dependabot config or `dotnet list package --vulnerable`)

### Priority 1 — Phase 5: Watchtower combat orchestrator decomposition

Already scoped in the Priority 1 plan (Steps 5.1–5.6). Recommended branch sequence:

1. `refactor/characterize-watchtower-combat-orchestrator` — responsibility map + characterization tests locking current behavior before any extraction
2. `refactor/extract-watchtower-target-and-movement-policy` — pure, internal, deterministic target-selection + movement-planning components
3. `refactor/extract-watchtower-action-resolution` — player-command resolution and attack resolution
4. `refactor/extract-watchtower-outcome-mapping` — `WatchtowerCombatOutcomeMapper`
5. `refactor/reduce-watchtower-combat-orchestrator` — reduce the top-level orchestrator to pure coordination
6. `feature/add-combat-write-facade` — a write-side facade mirroring the existing `CombatOperations`/`CombatView` read facade, so Console binds to it instead of `WatchtowerCombatRules`/`Watchtower*` intent types directly. **This is what actually closes the Console-coupling gap** flagged in the retrospective.

### Priority 2 — Dedupe cleanup

Small, low-risk, and prevents this duplication from multiplying once Phase 6 adds a second scenario:

7. `refactor/extract-core-participant-lookup` — collapse the 8-site `FindParticipant`/`FindParticipantIndex` duplication in `Core.Runtime`
8. `refactor/extract-application-mode-guards` — collapse the 4–6-site mode/progress-guard duplication in `Application`

### Priority 3 — Scope decisions to lock in writing before Phase 6 starts

Don't need full implementation yet — just a decision and a paragraph in this document, because Phase 6's design depends on the answers:

9. **Conditions system** — commit to full mechanical effects (Phase 10) or explicitly scope out with a written reason
10. **Spellcasting** — explicit yes / no / later. The party includes a Ranger (a 5e half-caster), so this needs an answer before scenario #2 gets authored around it
11. **Victory conclusion** — decide whether `WatchtowerScenarioConclusionValidator` gets a victory branch now (cheap, Phase 5/6-adjacent) or waits
12. ~~**Party composition genericity**~~ — **decided: campaign-declared.** Party composition belongs to a campaign, not to a scenario and not to the ruleset. The Scope Matrix's governing decisions settle it: players create their party in town from a supported catalog, keep a reserve roster, and replace dead characters, so a scenario cannot own a roster the player mutates between and during adventures. Party size is likewise a product decision, not a 5e rules fact — the ruleset says which classes exist, not how many adventurers make a party. Goals Phase 10 lists "party size" in the version-one product contract rather than among the things Phase 9 turns into content definitions.

    Consequences for the rest of Phase 6:

    - `ScenarioDefinition` carries **no roster**. Party composition is not scenario content.
    - Scenarios may still declare **entry requirements** — a predicate over the party ("at least two conscious members", "level 3+"). That is distinct from composition and does belong in the definition. The two compose: a campaign says parties here are 1–6, a scenario says you need at least 2 to attempt it.
    - There is **no campaign concept in the code yet** (no `Campaign` type anywhere in `src/`). Until there is, `WatchtowerPartyCompositionValidator` is the placeholder holding the current campaign's values.
    - This defuses a discrepancy worth knowing about: the Scope Matrix specifies four active characters (plus up to four reserve) while the code enforces exactly three (Fighter, Barbarian, Ranger). Once party size is campaign configuration rather than a constant, that becomes a data value to set rather than a code change, so it no longer blocks anything. **Still unresolved which of the two is stale.**

---

## Medium-term (next 1–2 months): De-center Watchtower — become an engine

**Goal:** execute Phase 6 for real, informed by the Priority 3 decisions above, and *prove* genericity by running a second, independently-authored scenario through the same engine with zero Watchtower-specific code paths.

### Phase 6 — Scenario Content Boundary (expanded scope per the retrospective)

1. `refactor/genericize-application-session-scenario-state` — replace `ApplicationSessionState.Scenario: WatchtowerScenarioState` with a generic `ScenarioState`/`ScenarioProgress` shape; Watchtower's specific progress enum becomes scenario-supplied data, not a hardcoded session-state field. **This is the actual de-centering step** — the one that makes the engine loadable with any scenario rather than merely organized around one.
2. `refactor/genericize-party-composition-rules` — **done (PR #89)**, deliberately decision-neutral: it moved the Watchtower party requirements out of `ApplicationSessionRules` without deciding where they ultimately live. Priority 3 item 12 has since answered that (campaign-declared), so the remaining work is a campaign-level home for them — which needs a campaign concept to exist first, and is therefore sequenced after this phase rather than inside it
3. `feature/add-scenario-definition-model` — immutable in-memory definitions: `ScenarioDefinition`, `LocationDefinition`, `EncounterDefinition`, `CombatantDefinition`, `ObjectiveDefinition`, `TransitionDefinition`, `RewardDefinition` (Priority 1 plan Step 6.2). Per Priority 3 item 12, these carry **no party roster**; scenario-level party constraints are expressed as entry requirements only
4. `feature/add-scenario-definition-validation` — validate content at load time: unique IDs, valid references, supported dice, reachable transitions, valid maps/coordinates, valid starting state (Step 6.3)
5. `refactor/move-watchtower-content-to-definition` — move authored Watchtower constants into a definition instance; remove scenario-name checks from execution code, renaming `CanEnterWatchtower`-style methods to generic verbs (Step 6.4)
6. `refactor/genericize-console-scenario-rendering` — update Console to render off the generic scenario definition plus the new Phase-5 combat write facade, closing the remaining Watchtower-coupling gap end to end
7. `test/prove-second-scenario-definition` — author a small but genuinely independent second scenario and prove: no hardcoded scenario-name checks fire, execution is fully data-driven, transitions work, a scenario-#2 save file loads correctly (Step 6.5)

### Phase 7 — Cross-layer die capability alignment

Priority 1 plan, can run in parallel with late Phase 6. Confirm every Core-supported die (D4/D6/D8/D10/D12/D20) resolves through Application's deterministic random source; reject undefined die types at content-load time — protects scenario #2+ authoring from silently broken dice.

### Phase 8 — Test infrastructure

Ongoing, not a gate — keep doing this throughout Phases 6 and 7 rather than as a separate pass.

---

## Long-term (finish the project): Become a game

**Goal:** everything the retrospective's "what it will take to finish" section flagged as unstarted. This is the largest and least-scoped horizon — each phase below will likely need its own detailed planning pass when it's reached, the way Phase 5 and 6 already have.

### Phase 9 — Real content authoring & a lightweight authoring workflow

- Go beyond the minimal Phase-6 proof scenario into a real, narratively complete second adventure
- Capture whatever authoring friction shows up as tooling/convention improvements — this is where it becomes clear whether the in-memory definition model needs an external format (the Priority 1 plan deliberately deferred that decision, Step 6.6)

### Phase 10 — 5e mechanical completeness

> **Superseded in part, 2026-07-26.** `docs/supported-scope-matrix.md` is marked *Accepted baseline* and commits **Prone** as the only reusable combat condition for the slice, alongside the health/lifecycle states. `docs/game-development-goals.md` states the governing principle directly: *"Do not implement every condition at once. Implement the conditions required by current spells, monsters, traps, and environments."* The "all 15 Conditions" item below was written before those documents were read, and does not govern. **Mechanics are built when authored content needs them.** Content leads; this phase follows Phase 9 rather than standing beside it.

- ~~Wire full mechanical effects for all 15 Conditions into attack/save/movement rules~~ — superseded; implement Prone properly, then whatever authored content actually requires (currently only immunity-gating exists)
- Execute the spellcasting scope decision from short-term Priority 3 — if "yes," this is a substantial new Core subsystem, not a small addition
- A structured rules-coverage audit against the two locally-held rulebook references, to surface further gaps before they're discovered mid-content-authoring

### Phase 11 — The Godot client

> **Scoped down, 2026-07-26.** `src/FiveEGoldBox.Godot/` is the user's in-progress work, untracked and actively edited. Phase 11 as executed here means **engine-side only**: the read models, facades and command surfaces a UI client needs, with tests, on the engine side of the boundary. Wiring `AppShell` is the user's.

The single largest remaining unit of work in the project:

- UI/UX design pass for party management, exploration, tactical combat, save/load, settings
- Wire `AppShell.cs` to the engine via the same generic read/write facades Console uses (`CombatView`/`CombatOperations` plus the Phase-5 write facade) rather than reinventing a parallel integration
- Art/asset pipeline — currently doesn't exist at all
- Input handling beyond the current placeholder keyboard shortcuts
- Decide Console's long-term role once Godot is real: stays a developer/debug tool, or continues as a secondary supported client

### Phase 12 — Production readiness

- Playtesting loop (automated tests prove internal consistency, not that the game is fun or well-paced)
- A design document beyond the Scope Matrix's committed/deferred feature list, to guide scenario #3+ content
- Execute the licensing decision from short-term housekeeping
- Telemetry/crash-reporting strategy if this will ever run on a machine the developer doesn't control
- Accessibility pass — cheapest to build into the Godot UI from the start rather than retrofit
- Close remaining documentation debt: XML doc coverage on public API entry points, any ADRs worth writing for major decisions made along the way

---

## How this is tracked

Same discipline as Priority 1: a living checklist in `CLAUDE.md`, phases checked off as branches merge, "known gap, deliberately deferred" notes kept honest rather than glossed over. Priority 1's remaining phases (5–8) and Priority 2's phases (9–12) share one continuous numbering so there's never ambiguity about sequence — this document exists to carry that numbering past where the original plan stopped, not to restart it.
