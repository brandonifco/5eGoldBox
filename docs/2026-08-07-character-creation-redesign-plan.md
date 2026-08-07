# Character creation redesign — research and plan (2026-08-07)

The first character-creation UI (PR #291) shipped a working wizard and was
correctly rejected on sight: every choice was a bare name in a list, so a player
who does not already know 5e is guessing blind. The panel was also a small modal
over the adventuring view, and ability scores were assigned one at a time with no
way to see the whole array first.

This doc records what the reference games actually did, what is worth copying,
and the plan that follows. Three decisions were taken with the user before any of
it was written; they are recorded in "Decisions" below.

## The central research finding: the Gold Box games are the wrong model here

This is worth stating first, because it inverts the obvious instinct.

Pool of Radiance was, in the Digital Antiquarian's retrospective, **"not
particularly welcoming to newcomers unfamiliar with Advanced Dungeons &
Dragons"** — it faithfully reproduced racial level caps, spell memorisation and
the rest of AD&D's complexity. Its on-screen character creation was terse
*because it did not have to explain anything*: the box shipped with a rulebook
and an Adventurer's Journal, and the explanation lived there.

We cannot inherit the printed manual. So copying the Gold Box creation screens
faithfully reproduces exactly the blind-guessing problem this redesign exists to
fix. **The aesthetic is worth copying. The information architecture is not.**

## What is worth copying

Four things, all of which the original genuinely got right:

1. **"Every option available to you at any given time is always displayed
   onscreen."** This is the Gold Box interface's own stated design principle, and
   it is the direct antidote to guessing. Nothing should be hidden behind a
   submenu the player has to know exists.
2. **A dedicated, full-screen Party Creation Menu, wholly separate from the
   adventuring interface.** Its options were Create New Character, Drop
   Character, Modify Character, Train Character, View Character, Add Character To
   Party, Remove Character From Party, Load, Save, Begin Adventuring. Creation
   was never a panel floating over the game world.
3. **A character *pool*, not a conveyor belt.** You created characters, then
   composed a party from them, and **Modify Character** let you go back and
   change one afterwards. PR #291's wizard marches through four characters in
   order with no way back to the first — a regression against a 1988 game.
4. **Ability scores were visible before they were committed** — freely
   rerollable, or directly enterable. The player saw the whole set and could
   change it.

Also confirmed as already-correct: shipping a **pre-generated party for
introductory play** (Pool of Radiance did; our authored roster is the
equivalent), and a **fixed party size** the creation flow fills.

## What has to be added, because the manual is gone

- **A description for every choice**, on screen, at the moment of choosing.
- **Recommendations**, because "which of these eighteen skills matters for a
  Wizard" is not answerable by a newcomer from names alone.

## Content findings

Investigated before planning, because they set the cost of the description work:

- **`~/5eData` has no prose whatsoever.** The sibling project is
  mechanically complete (9 races, 9 subraces, 12 classes, 40 subclasses, 13
  backgrounds) but every entry is structured mechanics plus a PHB page citation.
  There is nothing to port. Its own CLAUDE.md is explicit that the two projects
  are never wired together; it remains a useful cross-check, not a source.
- **The two PHB HTML files in `docs/` are copyrighted.** Local reference only —
  they must not be a source for shipped text.
- **SRD 5.1 is CC-BY-4.0** and carries real descriptive text for races and
  classes. It is thin on backgrounds (Acolyte essentially alone) and has no
  per-skill flavour, so those need original prose.
- **`primaryAbilityIds` is a modelled concept in 5eData's class data.** That is
  exactly the input a recommendation needs, which means recommendations can be
  *derived from content* rather than hardcoded in the UI.

## Decisions

Taken with the user, 2026-08-07:

1. **Description text: SRD 5.1 plus original prose.** SRD-derived for races and
   classes; original prose for backgrounds and skills, where the SRD is thin.
   Requires the standard CC-BY attribution notice in the repo, and in-game
   credit.
2. **Flow: a Party Creation Menu, Pool of Radiance-style.** A full-screen roster
   hub — create characters into a pool, review or modify any of them, then Begin
   Adventuring. Explicitly fixes the no-way-back weakness.
3. **Recommendations: label and explain, never preselect or restrict.** Mark
   suggested options and say *why* ("Recommended for Fighter — Strength drives
   your attack and damage rolls"). The player always chooses.

## Plan

### Phase 1 — content model (engine)

`Description` on `RaceDefinition`, `SubraceDefinition`, `ClassDefinition`,
`SubclassDefinition`, `BackgroundDefinition`, `SkillDefinition`, and
`PrimaryAbilities` on `ClassDefinition`.

**All optional**, so every existing content file keeps loading unchanged.

**The `Content/V1` DTO layer must be mirrored in the same change.** This is the
trap that bit the subclass work: `Core.Definitions` is not what content loads
through, so adding a field there alone produces a schema that does not change and
JSON that is silently dropped. Regenerate the schema via `ContentPackSchemaWriter`
and confirm the generated file actually contains the new fields before moving on.

### Phase 2 — author the descriptions

Every race, subrace, class, subclass, background and skill in
`data/rulesets/campaign/core.json`. Plus `PrimaryAbilities` per class. Plus a
`NOTICE`/attribution file carrying the CC-BY-4.0 notice for the SRD-derived text.

Verified by the existing validators and by a test asserting no shipped choice is
description-less — the guard that keeps the next authored race from silently
regressing to a bare name.

### Phase 3 — read seam

`CharacterCreationOptions` already carries the Core definitions, so descriptions
and primary abilities ride along at no cost. Two real gaps to close:

- **`ActivePartySize` is not publicly reported**, so the client hardcodes 4
  (flagged in PR #291). Needs a public accessor, same shape as `DescribeOptions`.
- **Recommendation derivation** belongs beside the options, not in the Godot
  layer, so it can be tested headlessly.

### Phase 4 — the screen

Replaces the modal entirely.

- A full-screen **Party Creation Menu**: the roster so far, with create / view /
  modify / remove per slot, and Begin Adventuring once full.
- A full-screen **character builder**: choices on one side, **the highlighted
  option's full description on the other**, and a **live character sheet** that
  updates as choices are made, so the consequence of a choice is visible before
  committing to it.
- **Ability scores: the whole standard array visible at once**, placed in any
  order, showing racial bonuses and the resulting scores, HP and AC live.
- Recommendation labels throughout, per decision 3.

Point buy stays deferred — it needs a running-budget control, and the array UI
above is its prerequisite anyway.

## Sources

- [Opening the Gold Box, Part 4: Pool of Radiance — The Digital Antiquarian](https://www.filfre.net/2016/03/opening-the-gold-box-part-4-pool-of-radiance/)
- [Pool of Radiance — Gold Box Games Wiki](https://wiki.goldbox.games/index.php/Pool_of_Radiance)
- [Pool of Radiance — Gold Box Wiki (Fandom)](https://goldbox.fandom.com/wiki/Pool_of_Radiance)
- [Pool of Radiance: Characters creation — StrategyWiki](https://strategywiki.org/wiki/Pool_of_Radiance/Characters_creation)
