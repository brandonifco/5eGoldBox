# Gold Box combat design reference — gap analysis

**Date:** 2026-08-04. Source document: `gold-box-style-combat-system-design-reference.docx` (user-supplied, "Designing a Gold Box–Style Tactical Combat Screen and Engine," v1.0, August 2026) — a 25-section behavioral spec plus five appendices, synthesizing SSI's Pool of Radiance/Treasures of the Savage Frontier/FRUA manuals into architecture guidance for a C#/Godot implementation.

**Method:** every claim below about the *current* engine is sourced to an actual file:line, gathered by a dedicated investigation pass (not assumed from memory or naming). Every claim about the *design doc* is the doc's own section number. Where the two conflict, the doc's own confidence marker (D/O/R/P — see its section "How to read this document") is carried through.

## Verdict up front

The design doc's single most important architectural recommendation — a deterministic, pure-C# domain layer that Godot only renders, never decides — is **already fully built and rigorously tested** in this engine (`ApplicationRandomSequence`, `CombatOperations`, golden-transcript characterization tests). That's arguably the hardest and most valuable 40% of the doc's entire recommendation, done. Several of the doc's own explicit "AD&D → 5e translation" calls (section 22) have also **already been made correctly**, independently, before this document existed.

What's actually missing maps cleanly onto specific, nameable gaps — not "redo the architecture," but "add features onto a foundation that's already right." The rest of this document is that gap list, prioritized.

## Part 1 — Already correct, validate and move on

Don't second-guess these; the doc would endorse all of them as-is.

| Doc topic | Doc's guidance | Current state |
|---|---|---|
| §17 domain architecture | Pure C# domain, no Godot references, deterministic, headless-testable | `Application`/`Core` have zero Godot dependency; `CombatView.cs` and partials contain no hit/miss/legality logic anywhere — confirmed by direct read, not inference |
| §24.1 deterministic RNG | One RNG service, seed + draw count, purpose-tagged, replayable | `ApplicationRandomSequence.GenerateDie` derives every roll from `SHA256(seed, valuesConsumed, sides, attempt)` — fully replayable from `(seed, valuesConsumed)` alone. Purpose tags (`CombatDiePurpose`) exist for logging/UI, matching the doc's intent even though they're layered on top rather than baked into the RNG call itself |
| §24.4 golden replay tests | "Known seed and command list yields exact event sequence... prevent accidental rules drift" | `WatchtowerCombatOrchestratorCharacterizationTests` + two frozen transcript fixtures, asserting revision numbers and RNG cursor/die values byte-for-byte. This is the doc's testing pyramid top tier, already in daily use |
| §22 table: descending AC/THAC0 → ascending AC | Use 5e's real attack math, don't keep AD&D math under a 5e label | Already 5e (ascending AC + attack bonus) |
| §22 table: random initiative/segments → one roll per combat | "Usually one initiative roll per combat; fixed order, ties resolved once" | Exactly this: one d20 per combatant at encounter start (`ScenarioEncounterFactory.Create`), fixed order for the whole fight, tie-break by roster position. No segments — correct, since segments are the doc's own AD&D-only, "don't copy" concept |
| §22 table: firing while adjacent | "5e... imposes disadvantage under specified conditions" — the doc explicitly calls out getting this wrong as *the* cautionary example (§22.1) | Already correct: `ResolveAttackRollMode` imposes disadvantage on a ranged attack with an adjacent hostile, doesn't block it outright (AD&D's rule, which this doc explicitly warns against copying) |
| §22 table: backstab by exact position → Sneak Attack by advantage | Advantage-or-adjacent-ally trigger, not "directly opposite the first attacker" | Already correct: `RollContributionCondition.AdvantageOrAdjacentEnemy`, textbook 5e trigger |
| §22 table: negative-HP bleeding → death saves | 5e death saving throws, not AD&D bleed-to–10 | Already correct, and complete: DC 10, 3/3 success-failure, nat 20 heals to 1 HP, nat 1 counts double, massive damage kills outright, crit-at-0-HP causes 2 failures |
| §9.3 spell interruption / concentration | Damage should be able to cancel a pending/ongoing spell via an explicit rule check, never inferred from animation | Already correct and complete: incapacitating damage auto-breaks concentration (no roll, per RAW), otherwise a real CON save vs. `max(10, damage/2)` |
| §6.5 camera behavior | Center on active unit at turn start; support edge-scroll/pan; one-key return; avoid violent recentering during chained reactions | Built this session: turn-change recentering (not every refresh), mouse-hover + keyboard-cursor edge-scroll, both push-away-from-edge rather than competing forces |
| §11.1 context-sensitive absence | Commands disappear rather than existing disabled | Already the model: `CommandBarController.ShowCommands` only ever receives currently-legal commands |
| §21.2 dual targeting: cycle vs. cursor | Both should call the same legal-target query | Move targeting's new keyboard cursor and mouse click both resolve against the same `CombatMovementDestinationOption` list |

## Part 2 — Real gaps, tiered by size and value

### Tier 1 — small, UI-only, no new domain work

**1. No initiative order visualization.** Turn order is a real, meaningful, already-computed `CombatTurnState.InitiativeOrder` — it's just never shown. The doc calls an "initiative strip or segment timeline" part of the *default* layout (§21.1), and separately names "weak visibility into initiative" as one of the specific historical weaknesses worth fixing, not preserving (§15.2). This is the single cheapest, highest-ratio item in this whole list: the data exists, it needs a Godot-side list/strip, nothing else.

**2. Attack/spell target preview depth not confirmed rich.** The doc (§21.3) wants hit chance, cover, advantage/disadvantage state, and expected damage range shown *before* commitment, generated from the same query that will validate the command. Current move targeting just got this treatment (legal/illegal + reason). Attack/spell targeting's actual preview depth wasn't nailed down by the investigation — worth a direct look before scoping, but plausibly a similarly small win once move targeting's pattern exists to copy.

### Tier 2 — real domain feature, moderate size, directly named a "strength worth preserving"

**3. Reactions / opportunity attacks — the biggest real gap.** `HasReactionAvailable`/`SpendReaction` exist as unused scaffolding; nothing triggers a reaction anywhere in the codebase. The doc calls guard/free-attacks/positioning-matters-between-turns one of the system's core strengths (§15.1) and a functional requirement (FR-007). Two things block this:
   - Movement is currently **atomic** (whole path validated and applied in one state revision) rather than **stepwise** (doc FR-005: resolve cell-by-cell, check reactions mid-path). Building real opportunity attacks means reworking movement resolution first.
   - This also unlocks a real 5e Ready action (replacing AD&D's per-round Delay, which the doc itself says to drop for a 5e profile — §22 table) and a Guard-equivalent, for free, once the reaction-window machinery exists.
   
   This is the one item on this list that's genuinely architectural, not additive — worth a dedicated scoping pass of its own before starting.

**4. No morale / flee-when-losing AI.** Currently: nearest-target heuristic, fights to the death, no morale of any kind. The doc frames morale as pure pacing value ("prevents every battle from requiring the last hit point of the last enemy," §12.4), explicitly *not* asking for a sophisticated AI upgrade (§12.3: "copy the automation feature, not the [AI's] limitations"). Small, self-contained, doesn't require touching the attack/movement pipeline.

### Tier 3 — real domain feature, larger, content-gated

These match the project's own already-stated philosophy (CLAUDE.md, Phase 10 scope decision 1: *"do not implement every condition at once — implement the conditions required by current spells, monsters, traps, and environments"*) — the doc doesn't disagree; §16 (FR list) describes the capability, but nothing here should be built ahead of content that actually needs it.

- **Geometric AoE spell templates** (radius/cone/line — §9.2, §19.4). Spells currently only support a flat additional-target *list*, not real area geometry. Only matters once a spell like Fireball/Burning Hands is actually authored (the ruleset is deliberately capped at six spells right now).
- **Persistent spatial effects** (webs, hazard zones, walls — §9.4). `ActiveEffect` only ever attaches to a combatant, never a cell. No current spell needs this.
- **Real condition-instance system** (Prone, Restrained, Frightened, etc. — §10.3). `ConditionType` and `ConditionRules.CanApplyCondition` exist but are never called; the codebase's own comment already admits "nothing yet applies a condition to a combatant who is still standing." This is a known, self-documented, *deliberately* deferred gap, not an oversight — the project's stated rule is content pulls mechanics, not the other way around.
- **Reinforcement waves** (§23.2, FR-009). `EncounterState.Participants` is fixed at encounter start; nothing can join mid-fight. The doc frames this as Treasures of the Savage Frontier's headline advance over Pool of Radiance — a real, well-documented feature, just not yet needed by any authored encounter.
- **Victory objectives beyond "wipe the enemy side"** (§23.3, FR-014). `EncounterCompletionRules` has exactly one win condition today. Escape/protect/survive-N-rounds are all doc-recommended and all absent.

### Explicitly out of scope — matches either the doc's own "don't copy" list or an already-made project decision

- **Extra Attack (Multiattack).** Gated behind a leveling system that doesn't exist — CLAUDE.md already recorded this as a deliberate, standing deferral, not a new finding here.
- **Facing / rear attacks / position-based backstab.** The doc itself frames this as AD&D-specific engine behavior a 5e game shouldn't reproduce (§22 table). Already correctly absent.
- **Ten-segment rounds / per-round reinitiative / Delay-as-repeated-rescheduling.** The doc's own confidence table (Appendix B) rates the *formula* for this low-confidence and explicitly says a 5e profile should use one roll per combat instead (§22). Already correctly not built.
- **Mid-combat save.** FR-011 wants it eventually, but the doc's own recommended development order (§25) puts save/replay at step 13 of 14 — after reactions, spells, AI, and objectives, not before. The current exclusion is deliberate and self-documented (`SaveActiveEncounterV1` doc comment explains the re-derivation reasoning); no conflict with the doc's own sequencing.
- **QUICK for player characters** (delegate a PC to AI). Named in the doc (§12.1) but not requested by anyone here; skip unless asked.

## Recommended next step

Given the size of tier 2/3 items, this isn't a "just go build it" list — it's a menu. My read: **initiative visualization (tier 1, #1)** is the highest ratio of value to effort and could ship today; **reactions/opportunity attacks (tier 2, #3)** is the single most tactically important gap and the one the design doc leans on hardest, but it's a real scoping exercise (stepwise movement rework) before any code.
