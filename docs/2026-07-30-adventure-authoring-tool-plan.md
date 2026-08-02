# Adventure authoring tool plan (map maker + content editor)

## Motivation

The user wants a full authoring/import pipeline for new adventures: a map maker for
towns/buildings/dungeons/caves — bounding exploration areas, tagging encounter/interaction/
decision tiles, doors, secret doors, treasure, entrances/exits/stairs between levels — plus tools
to author new items/weapons/spells and new monsters/NPCs, and tile art.

This sits directly on top of [`2026-07-30-data-driven-content-plan.md`](2026-07-30-data-driven-content-plan.md),
which is the locked-in plan for externalizing content into versioned JSON packs. **That plan is
now fully complete** (Phases 1–5, PRs #182–#198, landed concurrently with this plan's authorship
and Phase A below): ruleset, scenario, and campaign content are all real JSON under `data/`, loaded
through versioned DTOs/mappers, with generated JSON Schemas at `data/schemas/*.schema.json` and a
standalone `validate` console command. This document's own Phase D (below) described doing that
work for scenario content specifically — it's done, and done more completely than scoped (campaign
content got the same treatment too, which this plan hadn't called out by name).

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
  was authored inline, directly in each scenario's `EncounterDefinition.Combatants` — there was no
  reusable "bestiary" entry referenced by ID the way weapons/spells are. **Closed by Phase C
  below.**
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

### Phase A — Generalize `ExplorationFloor` (done, PR #187)

Mirrors the precedent already used for scenario progress (`ApplicationSessionState.Scenario`'s
`ProgressId` — an opaque string instead of a scenario-specific enum, Priority 1 Phase 6).
`ExplorationFloor` is gone; `Floor`/`StartingFloor`/`DestinationFloor` are plain `string`s, so a map
can declare any number of floors. Existing two-floor content kept working unchanged —
`"GroundFloor"`/`"UpperFloor"` are now literal string values instead of enum members, no behavior
change; the V1 save format stayed byte-identical. Public API 64 → 63 (the enum's removal),
reflection-diffed against `main` — confirmed nothing else moved. Landed *before* the scenario-pack
DTOs (Phase D, below), so the JSON scenario packs already carry `"Floor": "GroundFloor"` etc. as
plain strings — confirmed directly in `data/schemas/scenario-pack.schema.json`
(`ExplorationFloorDefinitionV1.Floor`, `StairDefinitionV1.DestinationFloor`,
`ScenarioTriggerDefinitionV1.Floor` are all `"type": "string"`, not an enum). No further work needed
here before a map can declare 3+ floors.

### Phase B — Door / secret door / treasure runtime state (done, PR #207)

New definition types (`DoorDefinition`, `TreasureDefinition`) plus new `ExplorationState` slots
(`OpenDoorIds`/`RevealedSecretDoorIds`/`CollectedTreasureIds`) and matching rules
(`CanOpenDoor`/`OpenDoor`, `CanRevealSecretDoor`/`RevealSecretDoor`,
`CanCollectTreasure`/`CollectTreasure`). Secret-door discovery is binary, as scoped — deliberately
not wired to a skill check. Treasure is flag-only (no inventory/currency system exists to grant
anything into); `IsLocked` is a permanent binary blocker with no unlock path. Watchtower's ground
floor gained a small additive appendage (an ordinary door plus treasure, a secret door, and a
locked door sealing the floor's one pre-existing interior gap) exercising all three mechanisms
through real, registered content — both frozen combat transcripts and the existing exploration
test suite passed unchanged, confirming the addition was purely additive. `Application.dll` held
at 64 exported types (everything added is internal). Godot rendering for the new cell kinds is
Phase G, below, still separate.

### Phase C — Monster/NPC bestiary extraction (done, PR #203)

`MonsterDefinition` (public, `Core.Definitions`, mirroring `WeaponDefinition`'s placement and
visibility exactly) is now a standalone bestiary entry — `EncounterCombatantDefinition` (replacing
`CombatantDefinition`) carries only `CombatantId`/`MonsterId`/`SideId`/`StartingPosition` and
references a monster by ID, the same way `CombatantWeaponDefinition.WeaponId` already references
the ruleset's `WeaponDefinition`. Six monsters extracted into `data/rulesets/campaign/core.json`
(Watchtower's two raiders, the Sunken Chapel's two guardians, Hollow Mill's giant rat and miller's
thrall) — the giant rat is shared by ID across both Hollow Mill encounters that use it, which is
the actual duplication this phase existed to remove. New load-time validation
(`scenario.combatants.monster_id_unresolved`) catches a `MonsterId` that doesn't resolve against
the scenario's ruleset — deliberately not retrofitted onto `WeaponId`, which stays exactly as it
was. Both frozen Watchtower combat transcripts passed unchanged, confirming the extraction is
byte-for-byte behavior-preserving. Public API: `Core.dll` 173 → 176 (the three new public types);
`Application.dll` held at 64 (everything added there is internal).

### Phase D — Scenario (and campaign) content pack loader (done, PRs #188–#198)

Done by the concurrent session working the data-driven-content plan itself, and done for all three
pack kinds, not just scenario: `ScenarioPackMapper`/`ScenarioPackLoader` (PRs #188–190),
`CampaignPackMapper`/loader (PRs #191–193), a `FIVEEGOLDBOX_DATA_ROOT` runtime override verified
against the real Godot engine (PRs #194–195), and — directly useful to Phases E/F below — generated
JSON Schemas for all three pack kinds at `data/schemas/{ruleset,scenario,campaign}-pack.schema.json`
plus a standalone `dotnet run --project src/FiveEGoldBox.Console -- validate <kind> <path>` command
(PRs #197–198). The three hardcoded scenario providers and the hardcoded campaign roster are
deleted; everything under `data/` is the only copy of that content now.

**This changes Phases E and F below from "build against a format that doesn't exist yet" to "build
against a real, validated, schema-documented format that's already on disk."** Neither phase needs
to invent or wait on a wire format anymore — `data/scenarios/*/scenario.json` and the matching
schema are the concrete target today.

### Phase E — Tiled importer (done)

A new `import-map` verb in `FiveEGoldBox.Console` (`TiledMapConverter.cs` + `TiledMapImportCommand.cs`,
mirroring the `validate` verb's own shape exactly): reads a Tiled `.tmj` export and merges it into
an existing scenario's JSON file as one location's `ExplorationMap` plus tile-anchored `Triggers`,
then reports whether the result validates via the Phase D `ContentPackValidation.ValidateScenarioPack`
facade — the same "report, don't gatekeep" philosophy `validate` already uses. Scoped to walkable
tiles, stairs, and triggers (encounter/interaction points), per the plan's own call to ship before
Phase B (doors/secret doors/treasure) rather than wait on it — those layer in once that content
exists to target. `V1` DTOs/mapper/loader are all `internal` to `Application` with no
`InternalsVisibleTo` reaching `Console`, so this works purely at the plain-JSON
(`System.Text.Json.Nodes`) level, never touching the internal pack types.

```
dotnet run --project src/FiveEGoldBox.Console -- import-map <tiled-file> <scenario-file> <location-id> <map-id> [<display-name>]
```

**The Tiled authoring convention** (one `.tmj` = one location's map):
- **Floors are tile layers** with a custom boolean property `IsFloor: true`; the layer's own `name`
  is the floor id string (any string, not just `GroundFloor`/`UpperFloor` — Phase A generalized
  that). A cell is traversable iff its GID, masked to clear Tiled's 3 high flip-flag bits
  (`gid & 0x1FFFFFFF`), is non-zero.
- **Exactly one `Start` object** (type/class `Start`, checked against both Tiled's `type` and newer
  `class` object fields for version resilience), properties `Floor` + `Facing`
  (`North|East|South|West`) — its own tile position (pixel ÷ tile size) becomes `StartingPosition`.
  Zero or more than one is a hard failure, nothing is written.
- **`Stair` objects**: properties `Floor` (source), `DestinationFloor`, `DestinationX`,
  `DestinationY` (ints — the destination isn't a placed object with its own pixel position).
- **`Trigger` objects**: properties `Floor`, `TriggerId`, `ResultingProgressId` required;
  `DisplayName` (defaults to the object's own Tiled `name`), `RequiredProgressIds`
  (comma-separated), `EncounterId`, `RequiredFacing` optional.

**Re-importing is idempotent by design, not by accident**: on merge, every existing top-level
`Triggers[]` entry for the target location that's *tile-anchored* (has both `Floor` and `Position`)
is removed before the freshly generated ones are appended — re-running the tool after editing the
Tiled map replaces its own output rather than duplicating it. Non-spatial triggers for the same
location (hand-authored story beats with no `Floor`/`Position`) are left untouched; they're not map
geometry. Verified end-to-end against a temp copy of the real `data/scenarios/hollow-mill/scenario.json`
(never the committed file itself).

### Phase F — Form-based content editor (done, PRs #209)

Scoped in detail in `docs/2026-08-02-content-editor-plan.md` (full decision record and phase
breakdown there). Built as a new `FiveEGoldBox.ContentEditor` Blazor Server project, referencing
`FiveEGoldBox.Application`/`Core` directly, with full CRUD forms for all four content kinds
(weapons, items, spells, monsters) reading and writing `data/rulesets/campaign/core.json`. Writes
go through raw JSON-node manipulation (mirroring Phase E's Tiled importer, not a round-trip
through the internal DTOs), validated on every save through the existing public
`ContentPackValidation.ValidateRulesetPack` before the file is replaced — a rejected save leaves
the committed file untouched. `RulesetJsonSplicer`/`RulesetJsonFormatting` reproduce the file's
hand-authored formatting exactly, proven by a no-op-save-is-byte-identical test. 20 new tests.

### Phase G — Godot tile rendering for new map content (done, PR #210)

`AreaMapView`'s existing single-`_Draw()`-pass grid now resolves each cell's paint color through a
precedence chain (stair > locked door > closed door > treasure > walkable > blocked), fed by three
new `ExplorationMapView` fields (`ClosedDoorPositions`/`LockedDoorPositions`/`TreasurePositions`)
that `ExplorationMapViewFactory` populates via door/treasure classification against
`ExplorationState`. An unrevealed secret door stays fully invisible (no visual hint); a locked door
gets its own distinct color from an ordinary closed door — both per prior design decisions in this
doc. Cell-color resolution is a pure, Godot-API-free method, covered by five new
`ExplorationRulesTests` cases against the real Watchtower scenario data. Tile *art* (as opposed to
flat colored cells) would still need a genuinely new tileset/atlas pipeline in Godot, since none
exists today; that remains separately scoped, unblocked but not started.

## Out of scope for this plan

- Leveling/multiclassing, condition-immunity enforcement, and the other product-backlog items
  already tracked in CLAUDE.md.

## Status (2026-08-02, updated after Phase G landed)

Phases A through G are all done. This plan is complete — the full authoring/import pipeline the
user asked for (map maker via Tiled import, door/secret-door/treasure runtime state, a monster
bestiary, JSON content packs, a form-based content editor, and Godot rendering for the new map
content) is built and merged. Remaining, genuinely separate work, not part of this plan's scope:

- Tile *art* for the area map (flat colors only today) — a new tileset/atlas pipeline, scoped
  separately once flat colors prove insufficient (see Phase G above).
- Scenario/campaign content editing (locations, encounters, triggers, roster) — the content editor
  is ruleset-only by design; see `docs/2026-08-02-content-editor-plan.md`'s "Out of scope."
