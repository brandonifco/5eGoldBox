# Reactions / opportunity attacks — implementation scoping

> **Status, 2026-08-10: Increment A is built and merged.** Read this doc for
> the analysis that shaped it, but note three places where reality diverged
> from the plan below, all recorded in CLAUDE.md's own entry:
> 1. **The frozen-transcript prediction in "Risks / verification" was wrong.**
>    The aggressive scripted run *does* retreat after adjacency (it attacks an
>    adjacent raider, then walks away), so the transcript changed and was
>    deliberately regenerated. The doc's insistence on actually running the
>    suite rather than assuming is exactly what caught it.
> 2. **Both sides got opportunity attacks, not just the enemies.** Increment B
>    is about surfacing a player *choice*; auto-taking the party's reaction
>    costs nothing today because no other reaction exists to save it for.
> 3. **Stepwise movement is scoped to paths that actually provoke.** A path
>    nothing reacts to still resolves in one atomic call and one revision.
>
> Increment B — a real player-facing "take the attack?" interrupt — remains
> unbuilt and unscoped, and only becomes a genuine decision once a second
> reaction option exists.

**Date:** 2026-08-05. Follow-up to `docs/2026-08-04-gold-box-combat-design-gap-analysis.md`'s tier-2 finding #3 ("the biggest real gap"), which flagged this as needing real scoping before any code — this is that scoping pass, not the implementation itself.

**Method:** every claim below is sourced to an actual file:line, read directly this session, not assumed from the gap-analysis doc's own summary.

## What already exists (unused scaffolding)

- `CombatTurnResources.HasReactionAvailable` (`Core/Rules/CombatTurnResources.cs:9`) and `CombatTurnResourceRules.SpendReaction` (`Core/Rules/CombatTurnResourceRules.cs:60`) are real, tested, and reset every turn by `StartTurn` — but nothing in the codebase ever calls `SpendReaction`.
- `EncounterActionDiscoveryRules.EvaluateReaction` (`Core/Runtime/EncounterActionDiscoveryRules.cs:365`) checks `HasReactionAvailable` and then unconditionally returns `EncounterActionUnavailabilityReason.ReactionWindowRequired` — a stub for a "reaction window" concept that doesn't exist anywhere else in the engine.
- Threat/reach primitives partially exist and are reusable: `EncounterWeaponAttackPrerequisiteRules.CalculateDistanceFeet` and `DefaultMeleeReachFeet = 5` (falling back from `weapon.ReachFeet`) already compute "is X within melee reach of Y" for the unrelated `AdvantageOrAdjacentEnemy` Sneak Attack condition (`TargetHasAdjacentEnemy`, line 196). The same primitive is what "does this square leave someone's threat range" needs — it isn't built as a general query yet, but the math it would be built from is already proven and tested.

## The two real blockers, concretely

### 1. Movement is atomic, and the whole call stack assumes it

`EncounterMovementRules.Resolve` (`Core/Runtime/EncounterMovementRules.cs:10`) takes a full `Path` and, in one loop with no observation point, validates and applies every step, spends the total movement cost, and returns one `EncounterMovementResult` at one new `Revision`. `WatchtowerPlayerCommandResolver.Resolve(..., CombatMoveIntent)` (`Application/Combat/WatchtowerPlayerCommandResolver.cs:14`) calls it once and wraps the single result into one `PrimaryStep`. There is no point in this pipeline where anything could pause mid-path to ask "did this step leave a threatened square?"

**This is less alarming than it sounds, because of one thing that already exists:** `CombatResolutionResult` already separates `PrimaryStep` from `AutomaticSteps: IReadOnlyList<WatchtowerCombatStepResult>` (`WatchtowerCombatOrchestrator.cs:224-225`), and `WatchtowerAutomaticTurnProcessor.ProcessUntilDecision` already appends multiple steps from a single player command's aftermath (enemy turns, deaths, completion). **The result model already knows how to represent "one player command produced several transcript steps."** The missing piece is only inside `EncounterMovementRules.Resolve` itself: it needs to walk the path one cell at a time (it already does, internally — the `foreach (GridPosition position in path)` loop at line 79 — just without yielding per-step), and after each step, ask whoever holds threat range over the vacated square whether they want to react, before continuing.

The concrete implementation-shape decision is: does `EncounterMovementRules.Resolve` grow a per-step callback/result-list (returning `IReadOnlyList<EncounterMovementStepResult>` instead of one final result), or does the *caller* (`WatchtowerPlayerCommandResolver`) re-invoke a single-step version of `Resolve` once per path cell in a loop, checking for reactions between calls? The second keeps Core's method signature closer to what it is today and matches this codebase's existing preference (seen throughout Priority 2) for pushing orchestration into Application and keeping Core rules narrow — recommend that shape unless prototyping shows it's awkward.

### 2. Weapon attack resolution hard-requires the actor to be the active combatant, and spends an Action

`EncounterWeaponAttackPrerequisiteRules.Evaluate` (`Core/Runtime/EncounterWeaponAttackPrerequisiteRules.cs:86-94`) rejects any attack where `actorCombatantId != state.ActiveCombatantId` (`ActorNotActive`), and separately requires `HasActionAvailable` (line 104), not a reaction. **Both of these are exactly backwards for an opportunity attack**, which is by definition taken by a non-active combatant, spending their reaction. The actual d20-roll/damage/crit math underneath (further down the same file, in `EncounterWeaponAttackRules.cs`) is generic and reusable — only the two gate checks at the top are wrong for this case.

The reuse-preserving shape (matching this codebase's own stated economy-of-code principle — "extend an existing path... don't duplicate dice or modifier calculation") is a trigger parameter threaded through the prerequisite check: something like an `AttackTrigger { Action, OpportunityReaction }` that swaps which gate applies (`ActorNotActive`+`HasActionAvailable` for `Action`, "actor is not the mover and is hostile to them"+`HasReactionAvailable` for `OpportunityReaction`) while the roll/damage resolution underneath stays untouched. This avoids a parallel `EncounterOpportunityAttackRules` that would silently drift from real weapon-attack behavior (crits, advantage/disadvantage, cover) over time.

## What a minimal first slice should and shouldn't include

Recommend splitting this into two increments, not one:

**Increment A — enemies get opportunity attacks against the player; no player reaction decision UI yet.**
- Stepwise movement resolution (blocker 1) + the attack-trigger parameter (blocker 2), but the *reacting side only ever auto-takes the attack if legal* — the same "simple, explainable" policy this project already uses for enemy turns (`EncounterTacticsPolicy`) and for downed-target advantage. No reaction offered/declined UI, no "should I take this or save it" logic.
- New Disengage action needed alongside this, or opportunity attacks become a pure tax on all player movement with no counterplay — 5e's own answer to "how do you leave a threatened square safely." `CombatTurnResources`/action-discovery already has the shape for a new action kind; this is additive, not a rework.
- This increment alone unlocks the doc's own two "for free" follow-ons once the reaction-window machinery exists: a real Ready action and a Guard-equivalent (both noted in the gap-analysis doc, not scoped further here).

**Increment B — the player's own combatants can react to a retreating enemy** — this is the harder half: it means pausing whatever's mid-flight (an AI's own movement resolution, or another player action) to surface a real "take the attack?" decision to the human player before continuing, which is a genuine UI/turn-control question (a modal interrupt mid-enemy-turn), not just a Core rules question. Recommend treating this as its own follow-up scoping pass once Increment A is live and played, not bundled in now — matches this project's own repeated pattern of shipping the AI-decides half of a feature first and coming back for the player-decides half (e.g. spellcasting's single-target-first, multi-target-later split).

## Risks / verification

- **Frozen combat transcripts:** predicted (not confirmed) safe from Increment A without regeneration. Grepped both scripted characterization runs (`tests/FiveEGoldBox.Application.Tests/WatchtowerCombatOrchestratorCharacterizationTests.cs`) — their movement paths are constructed toward engagement, not retreat-after-adjacency, so opportunity attacks likely never trigger in either the aggressive or passive scripted run. **Must verify by actually running the suite after implementation**, not assumed — if either transcript does move an already-engaged combatant away, both fixtures need the same reviewed regeneration process already documented for Sneak Attack/Bless (`[Fact(Skip=...)]`, un-skip, run, re-skip).
- **Content risk, separate from engine risk:** none of the three authored scenarios' encounters were designed with opportunity attacks in mind. Watchtower/Sunken Chapel/Hollow Mill's battlefields may now play differently (a melee enemy free-hitting a retreating party member) — this is correct 5e behavior, not a bug, but worth flagging to the user as a real gameplay-feel change to watch for in the next live playtest, the same way the isometric battlefield resize changed AI behavior in a way that needed calling out rather than silently absorbed.

## Recommendation

Increment A is a real, boundable branch (or two: stepwise movement + attack-trigger parameter could split into separate PRs if either proves larger than expected once started) — not a redesign of combat. Increment B is explicitly out of scope until A is played and confirmed working. Not started; flagging back to the user per the gap-analysis doc's own instruction not to just pick a direction here alone.
