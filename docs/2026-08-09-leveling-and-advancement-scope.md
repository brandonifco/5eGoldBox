# Leveling and advancement — scope

Prompted by an independent architecture review's finding that "level 1 is a much bigger
limitation than it looks." Investigated directly against the actual code before scoping
anything — the review's framing turned out to overstate the gap. This is a scope, not a plan to
implement yet; nothing here has been built.

Decided with the user before writing this: **classic XP totals** (not milestone), and **all
four active classes to level 2** as the first slice (not a single-class proof of concept, not a
full design pass to level 5).

## 1. What's already there — corrects the review

Traced the real call paths rather than trusting the audit. The engine's leveling *math* is
already built and already correct; nothing above it has ever driven it past level 1.

- `CharacterDraft.Level` (`Core/Characters/CharacterDraft.cs`) already exists, defaults to 1.
- `ProficiencyRules.GetBonus(level)` is already real, correct RAW 5e math
  (`2 + (level-1)/4`, bounded 1-20), and `CharacterResolver` already calls it with `draft.Level`.
- `CharacterResolverHitPoints` already computes
  `firstLevelHitPoints + (level-1) × additionalHitPointsPerLevel` — already level-general.
- `ClassDefinition.FeaturesByLevel` is already resolved cumulatively:
  `CharacterResolver.cs:160-165` does `.Where(pair => pair.Key <= draft.Level)`. A level-5
  character would already receive every feature from levels 1 through 5, mechanically, today.

The only thing stopping any of this from running above level 1 is one explicit gate:
`CharacterCreationRules.CheckCreationScope` rejects `draft.Level != 1` for *player-created*
characters, and its own comment says this directly — `CharacterResolver.Validate` "happily
accepts a level-6 Manual-scored character... player creation is narrower than what the draft
shape as a whole allows." A deliberate scope decision, not an engine limitation.

## 2. What's actually missing

1. **No advancement subsystem at all.** Nothing in `Application` ever changes a character's
   level after creation — no XP tracking, no level-up trigger. `PartyMemberState` has no
   `Level`/XP field of its own; level only lives inside the creation-time `CharacterDraft`,
   which nothing currently mutates through play.
2. **Spell slots have no character-level table.** `SpellSlotsByLevel` is keyed by *spell-slot
   level* only (1st-level slots, 2nd-level slots...) and `CampaignResourceGrants.ForClass
   (rulesetId, classId)` grants a flat count with no character-level parameter at all — a
   level-1 and level-9 Cleric would get identical slots today.
3. **No content past level 1 is authored** — every class's `FeaturesByLevel` has only a
   level-1 entry.
4. **No rest system exists anywhere** (`Encamp` is mock-only per the regional-travel gap
   analysis; confirmed here by direct search — zero `ShortRest`/`LongRest` hits in the whole
   engine). `CharacterResourceState` (spell slots, and anything else spent-and-recovered) is
   granted once at party creation and never replenished. This matters for level 2 content --
   see §4.
5. **Subclass mechanics are identity-only**, deliberately (2026-08-05 entry) — no mechanical
   effects exist for any of the 8 subclasses yet.
6. **No ASI/feat concept anywhere.** Out of scope for this slice regardless (ASIs land at
   level 4 at the earliest).

## 3. Advancement model (decided)

**Classic XP, shared party-wide total and level — not per-character.** Mirrors the precedent
`PartyState.Currency` already set ("a shared party purse, not per-character -- a deliberate
simplification"): the whole party fights every encounter together with no benching, so a
shared pool produces the same practical result as per-character tracking without the
bookkeeping (four independent XP totals, four independent level-up moments, four independent
spell-slot tables potentially out of sync). `PartyState` gains `ExperienceTotal: int` and
`Level: int` (defaulting 0/1); `SavePartyV1` gains matching optional fields defaulting to
0/1, so every pre-existing save fixture still loads unchanged, the same convention
`SaveCurrencyV1` already established.

**XP source:** `MonsterDefinition` gains `ExperienceValue: int` (required for new content, a
reasonable authored default backfilled for the 6 existing monsters using real 5e CR-derived
values for their approximate power level -- mill rat/similar trivial monsters land near the
CR 0 value of 10). Awarded in `CombatOutcomeRules.Finalize` on `isPartyVictory`, summing
`ExperienceValue` for every defeated combatant on the non-party side (`encounterDefinition
.Combatants` already carries `MonsterId`, resolved against the ruleset the same way
`CampaignResourceGrants` already does).

**Threshold table:** real 5e XP-to-level thresholds, levels 1-5 authored now for headroom even
though this slice only drives 1→2 (300 XP):

| Level | XP required |
|---|---|
| 1 | 0 |
| 2 | 300 |
| 3 | 900 |
| 4 | 2,700 |
| 5 | 6,500 |

**Level-up mechanics**, all in `CombatOutcomeRules.Finalize` right after XP is added: compare
new total against the threshold table; if it crosses one, bump `PartyState.Level`, re-resolve
every member's `CharacterSnapshot` (max HP grows via the already-level-general resolver, the
delta is added to *current* HP too so leveling up mid-adventure doesn't leave the party at a
now-lower fraction of their new max — matching how 5e actually plays this at the table), and
grant any new `CharacterResourceState` entries the new level's spell-slot table or feature
resources call for.

## 4. Per-class level 2 content — a real fork, not uniform

Real 5e's level 2 features for these four classes are not equally buildable today. Traced each
one against what the engine actually supports before proposing a shape:

- **Fighter — Action Surge (real, buildable now).** Turn-scoped `HasActionAvailable`/
  `HasBonusActionAvailable`/`HasReactionAvailable` already exist generically in
  `CombatTurnResourceRules` — nothing spell-specific about them. A once-per-something
  `CharacterResourceState` resource that, when spent, resets `HasActionAvailable` to `true`
  mid-turn is a real, small addition, not a new subsystem. "Once per rest" degrades honestly to
  "once, ever, until a rest system exists" — the same non-recovering reality spell slots
  already live in today, so this isn't a new inconsistency.
- **Rogue — Cunning Action (real, buildable now).** Same reasoning: a bonus-action-consuming
  Dash/Disengage/Hide option is new wiring, not a new subsystem, since bonus actions are
  already a tracked per-turn resource.
- **Wizard — Arcane Recovery (blocked, ships identity-only).** Its entire effect is "recover
  spell slots on a short rest" — with no rest trigger anywhere in the engine, this would be
  genuinely inert if built today. Ships the same way subclasses do: named, described, granted,
  mechanically dormant, with a doc comment saying why — not half-built, not silently dropped.
- **Cleric — Channel Divinity (blocked, ships identity-only).** Its actual effect is
  subclass-specific (Life Domain's Preserve Life, War Domain's Guided Strike/War God's
  Blessing), and subclass mechanics are deliberately unbuilt (2026-08-05). Same treatment as
  Arcane Recovery.

**Net: 2 of 4 classes get a real, playable new level-2 mechanic; 2 ship identity-only,
matching the subclass precedent exactly rather than inventing a new half-built category.**
This seemed worth surfacing before writing any code rather than silently picking one path —
happy to build all four as identity-only instead if a uniform first slice matters more than
Action Surge/Cunning Action actually working.

## 5. UX for this slice

Minimal, matching this project's own "content before polish" discipline: a level-up produces a
journal line (`AppendJournal`, the existing continuous-transcript mechanism) — "The party
reaches level 2!" plus a per-member HP-gain note — not a dedicated level-up screen. A real
"here's what your character gained" screen is a reasonable follow-up once there's an actual
level 3+ to show off, not part of this slice.

## 6. Explicitly out of scope for this slice

- A rest system (short/long rest, resource recovery) — Arcane Recovery and any future
  rest-gated content stay blocked on this until it's scoped separately.
- ASIs/feats (earliest real placement is level 4).
- Real subclass mechanics (Channel Divinity's actual effects, Champion's improved crit, etc.).
- Levels 3+ (the threshold table has headroom, but no level-3+ content is authored this pass).
- Per-character XP divergence / individual leveling.
- A dedicated level-up UI screen.
- Monster XP rebalancing beyond authoring `ExperienceValue` for existing content.

## 7. Rough phasing

1. **Engine:** `PartyState.ExperienceTotal`/`Level`, save format fields, `MonsterDefinition
   .ExperienceValue`, the threshold table, XP award + level-up in `CombatOutcomeRules.Finalize`,
   re-resolution on level-up. Provable headless the same way `CharacterCreationRules` was —
   a throwaway console/test harness driving a fight to victory and asserting the party leveled,
   before any journal/UX wiring.
2. **Spell slot re-authoring:** `SpellSlotsByLevel` becomes level-keyed (or a parallel
   level-keyed table) for Cleric and Wizard; `CampaignResourceGrants.ForClass` gains a
   character-level parameter.
3. **Content:** author level-2 `FeaturesByLevel` entries for all four classes; build the two
   real mechanics (Action Surge, Cunning Action); document the two identity-only ones.
4. **Presentation:** the journal line. No Godot structural change expected beyond that.
5. **Verify against real content:** author `ExperienceValue` for the 6 existing monsters,
   confirm a real fight (Watchtower or Hollow Mill) can actually carry the party from 0 to 300
   XP and level up correctly, HP/proficiency/features all landing right on a resolved
   `CharacterSnapshot`.
