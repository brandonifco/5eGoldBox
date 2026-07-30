# Adventure authoring tool plan (map maker + content editor)

## Motivation

The user wants a full authoring/import pipeline for new adventures: a map maker for
towns/buildings/dungeons/caves — bounding exploration areas, tagging encounter/interaction/
decision tiles, doors, secret doors, treasure, entrances/exits/stairs between levels — plus tools
to author new items/weapons/spells and new monsters/NPCs, and tile art.

This sits directly on top of [`2026-07-30-data-driven-content-plan.md`](2026-07-30-data-driven-content-plan.md),
which is the locked-in plan for externalizing content into versioned JSON packs. That plan's
Phase 1 (ruleset content DTOs/mapper/loader) is being implemented on its own branch,
`feature/ruleset-pack-dto-mapper-loader`, concurrently with this plan's authorship. This document
sequences around that rather than duplicating it.

## Decisions locked in (2026-07-30, via user confirmation)

1. **Sequencing:** the data-driven content plan's schema/loader work is the foundation the
   authoring tool builds on, not a parallel format of its own.
2. **Map maker:** integrate the existing open-source **Tiled** map editor rather than build a
   bespoke tile-painting GUI from scratch — author maps there, convert its export into this
   engine's own scenario-pack JSON with a purpose-built importer.
3. **Content editor** (monsters/items/spells/weapons): a small **form-based tool**, not raw JSON
   hand-authoring.

## Findings that shape the phases below

- **No door, secret door, or treasure/loot concept exists anywhere in the engine today**
  (confirmed by broad grep across `Core`/`Application`). The closest analog is
  `ScenarioTriggerDefinition` (location + optional floor/position/facing, gated by progress-marker
  strings, optionally starting an encounter) — this already *is* the engine's generic "something
  happens at this tile" mechanism. **"Encounter/interaction/decision point" tagging needs no new
  engine type** — it's already representable; what's missing is a way to *author* it from a map
  file instead of hand-written C# provider classes. That's the Tiled importer's job (Phase E), not
  new engine schema.
- **Exploration maps have exactly two floors, hardcoded**: `ExplorationFloor` was a fixed
  `GroundFloor | UpperFloor` enum (`src/FiveEGoldBox.Application/Exploration/ExplorationFloor.cs`).
  Multi-level dungeons/caves — squarely part of this request — could not be expressed. **Closed by
  Phase A below.**
- **Walkability is a sparse allow-list** (`ExplorationFloorDefinition.TraversablePositions`), not a
  dense grid. Doors/secret doors need a tile's traversability to change at *runtime* (open/closed),
  which today's `ExplorationState` has no slot for — this is new runtime exploration state, not
  just new content fields (Phase B).
- **No monster/NPC content type exists separate from `CombatantDefinition`.** Encounter opposition
  is authored inline, directly in each scenario's `EncounterDefinition.Combatants` — there is no
  reusable "bestiary" entry referenced by ID the way weapons/spells are (Phase C, flagged as
  needing a user decision, not assumed).
- **No tileset/atlas/tile-art pipeline exists in Godot at all.** Current art is one full-frame PNG
  per scene, or a flat placeholder color. `AreaMapView` already draws its grid via one `_Draw()`
  pass with flat colors, which extends cleanly to more tile "kinds" without an architecture change
  (Phase G) — but there's no existing sprite/tileset convention to build real tile *art* on top of;
  that would be genuinely new Godot work, scoped separately once flat colors prove insufficient.
- **Secret-door detection is deliberately not wired to a skill check in v1.** CLAUDE.md already
  tracks "wire the six functionally-dead `Core.Rules` classes (including `SkillCheckRules`) or
  leave them inert" as an open decision the user hasn't made. Folding that decision in sideways via
  secret doors would presume an answer to a question already flagged as pending — v1 secret doors
  are binary (adjacent + interact reveals them), no roll.

## Phased plan

### Phase A — Generalize `ExplorationFloor` (in progress, branch `refactor/generalize-exploration-floor-identifier`)

Mirrors the precedent already used for scenario progress (`ApplicationSessionState.Scenario`'s
`ProgressId` — an opaque string instead of a scenario-specific enum, Priority 1 Phase 6).
`ExplorationFloor` becomes a `string` floor identifier instead of a fixed 2-value enum, so a map
can declare any number of floors. Existing two-floor content keeps working unchanged —
`"GroundFloor"`/`"UpperFloor"` become literal string values instead of enum members, no behavior
change. Unblocks everything else map-related: doors, stairs, and the Tiled importer all need "more
than 2 floors" to be real before they're worth building against.

### Phase B — Door / secret door / treasure runtime state

New definition types (`DoorDefinition`: position, blocks-a-tile-until-opened, `IsSecret: bool`,
`IsLocked: bool`; `TreasureDefinition`: position, item/gold reward) plus new `ExplorationState`
slots (open/closed per door, found/hidden per secret door, collected/not per treasure) and rules
for interacting with them. Secret-door discovery is binary for v1 (see Findings above) —
deliberately not wired to a skill check.

### Phase C — Monster/NPC bestiary extraction (needs a user decision, not started)

Extract a standalone `MonsterDefinition` from `CombatantDefinition` (minus `StartingPosition`),
referenced by ID from `EncounterDefinition.Combatants`, so monsters become reusable/importable
content instead of copy-authored per encounter — mirrors how
`CombatantWeaponDefinition.WeaponId` already references the ruleset's `WeaponDefinition`.

### Phase D — Scenario content pack loader

Once the ruleset-pack loader (`feature/ruleset-pack-dto-mapper-loader`, the data-driven-content
plan's own Phase 1) lands, do the equivalent for scenario content (that plan's Phase 2): versioned
DTOs for all ~17 scenario definition types plus the new Phase B/C types, a `ScenarioPackMapper`,
structural-equality verification against the three existing hardcoded providers, then point
`ScenarioDefinitionRegistry` at the file-based loader.

### Phase E — Tiled importer

A small standalone console tool that reads Tiled's JSON (`.tmj`) export — tile layers become
`TraversablePositions`/wall tiles, object layers with custom properties become doors/secret
doors/treasure/stairs/`ScenarioTriggerDefinition`/`ScenarioDecisionDefinition` placements — and
emits the scenario-pack JSON from Phase D. This is the actual "map maker" from the user's ask:
Tiled provides the drawing/tagging UI, this tool provides the translation.

### Phase F — Form-based content editor

A small tool (likely a minimal ASP.NET Core web app, referencing `FiveEGoldBox.Application`/`Core`
directly so it validates through the exact same `RulesetValidator`/`ScenarioDefinitionValidator`
the engine uses, rather than reimplementing validation) with forms for weapons/spells/items/
monsters, reading and writing the JSON packs from Phases D/C directly.

### Phase G — Godot tile rendering for new map content

Extend `AreaMapView`'s existing single-`_Draw()`-pass grid (currently walkable/stair/blocked as
three flat colors) with more cell kinds for doors/secret-doors-once-found/treasure. Tile *art* (as
opposed to flat colored cells) would need a genuinely new tileset/atlas pipeline in Godot, since
none exists today; scope that separately once it's clear flat colors aren't enough.

## Out of scope for this plan

- The ruleset content pack loader itself (`Content/V1/`, `RulesetPackMapper`) — owned by
  `feature/ruleset-pack-dto-mapper-loader`, cited here only as the dependency Phase D waits on.
- Leveling/multiclassing, condition-immunity enforcement, and the other product-backlog items
  already tracked in CLAUDE.md.
