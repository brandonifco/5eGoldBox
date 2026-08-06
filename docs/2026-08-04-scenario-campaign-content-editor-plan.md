# Scenario/campaign content editor plan

## Motivation

The adventure-authoring-tool plan (`docs/archive/2026-08-02/2026-07-30-adventure-authoring-tool-plan.md`,
Phases A-G) closed out 2026-08-02 with scenario/campaign content editing explicitly
deferred: "the content editor is ruleset-only by design" (see that plan's own "Status"
section, and `docs/archive/2026-08-02/2026-08-02-content-editor-plan.md`'s "Out of
scope"). Authoring a new location, encounter, trigger, route, decision, or shop -- or
editing the campaign roster -- still means hand-editing `data/scenarios/*/scenario.json`
or `data/campaigns/frontier/campaign.json` directly. This plan extends the existing
`FiveEGoldBox.ContentEditor` Blazor Server app (previously weapons/spells/items/monsters
only) to cover scenario and campaign content too, following the same CRUD-forms-over-
JSON-files shape it already established.

## Why this is bigger than the ruleset editor was

The ruleset editor covered four flat-ish record shapes. A scenario file has roughly nine
distinct nested content kinds (`Locations` with a nested `ExplorationMap`/`Floors`/
`TraversablePositions`/`Stairs`/`Doors`/`Treasures`/`Npcs`, `Routes`, `Encounters` with
`Combatants`/`Outcome`, `Triggers`, `Decisions` with `Options`, `Shops` with `Items`,
plus scenario-level `Progress`/`Conclusions`/`PartyRequirement`), and campaigns add a
roster with ability scores, ammunition, and prepared spells. Building all of it as one
PR would be a bad slice, so delivery is phased the same way the original authoring plan
phased A-G.

## Key design decisions (2026-08-04)

1. **Read side: `InternalsVisibleTo` from `FiveEGoldBox.Application` to
   `FiveEGoldBox.ContentEditor`/`.Tests`.** The scenario/campaign V1 pack DTOs
   (`ScenarioPackV1`, `ScenarioLocationDefinitionV1`, `TravelRouteDefinitionV1`,
   `ShopDefinitionV1`, etc., under `src/FiveEGoldBox.Application/Content/V1/`) are
   `internal`, unlike the ruleset editor's four content kinds, which are public
   `Core.Definitions` types with no such grant needed. Rather than hand-roll a second,
   drift-prone set of mirror types purely to read JSON that's already schema-shaped by
   these DTOs, the editor is granted friend access and deserializes straight into them
   -- the same pattern `RulesetPackDocument` already uses for the public ruleset types.
2. **Write side: the same byte-preserving splice discipline, unchanged.**
   `RulesetJsonSplicer.ReplaceRootPropertyValue` (root-property-name + replacement text
   -> new document bytes, leaving every other byte untouched) is reused as-is for
   `Locations`/`Routes`/`Shops`. One addition: `ReplaceOrInsertRootPropertyValue`, for
   `Shops` specifically -- `ScenarioPackV1.Shops` defaults to empty, and most scenario
   files were authored with no `"Shops"` key at all (Watchtower, Sunken Chapel both have
   none), so the first shop ever saved into such a file has to insert the property (after
   the always-present `Decisions` field), not just replace it.
3. **New formatter, sharing primitives with the ruleset one.** `RulesetJsonFormatting`'s
   generic layout helpers (`RenderExplodedObject`, `RenderCompactArray`, etc.) were
   extracted into a shared internal `JsonLayoutPrimitives` class (a pure refactor,
   verified byte-for-byte via the existing `NoOpSaveFormattingTests`). `ScenarioJsonFormatting`
   builds `Locations`/`Routes`/`Shops` on the same primitives, plus one convention
   specific to scenario content: a compact array of progress-id strings
   (`ExplorableProgressIds`/`RequiredProgressIds`) stays on one line for one or two
   entries and only explodes at three or more -- one item looser than the ruleset
   convention (`Properties`/`AbilityModifiers` explode at two or more) -- confirmed
   against real content (a two-entry `ExplorableProgressIds` and a two-entry `Trigger`'s
   `RequiredProgressIds` both stay inline; every three-plus-entry list explodes).
4. **`ExplorationMap` is preserved verbatim, not re-rendered.** This turned out not to
   be a choice so much as a discovered necessity: real committed content orders a
   floor's `Doors`/`Treasures`/`Npcs` inconsistently between files (Hollow Mill writes
   `Npcs` before `Doors`/`Treasures`; Watchtower writes the opposite), so no single fixed
   field order reproduces every file's `ExplorationMap` byte-for-byte through a DTO-based
   renderer. `ScenarioPackDocument.FindRawExplorationMapText` instead locates and copies
   the exact original bytes for each location's map by `LocationId` via a targeted
   `Utf8JsonReader` scan, sidestepping the need to reverse-engineer that ordering (or
   care about it) at all. This is also why `ExplorationMap` isn't editable through this
   editor yet -- see Phase 4.
5. **Validation**: the already-public `ContentPackValidation.ValidateScenarioPack(path)`
   -- write-temp/validate-temp/copy-on-success/delete-temp-in-finally, identical
   structure to the ruleset editor's own write path.
6. **Discovery**: `RepositoryLocator.ResolveScenarioPackPaths()` scans
   `data/scenarios/*/scenario.json` (a scenario is one file per scenario, unlike the
   single hardcoded ruleset file), feeding a picker landing page (`/scenarios`).

## Phase 1 -- done, 2026-08-04: foundation + Locations + Routes + Shops

Shipped: the `InternalsVisibleTo` grant; `JsonLayoutPrimitives` extraction;
`RepositoryLocator.ResolveScenarioPackPaths`; `ScenarioPackDocument`
(read + raw-`ExplorationMap`-span lookup); `ScenarioJsonFormatting`; `ScenarioContentService`
(list/load/find/save/delete for Locations, Routes, Shops, scoped per scenario file path,
not a single fixed path); form models (`ScenarioLocationFormModel`, `TravelRouteFormModel`,
`ShopFormModel`) -- all `internal`, not `public`, since their DTOs are internal and a
public method with an internal parameter/return type is an accessibility-consistency
error; and the Blazor pages (`/scenarios`, `/scenarios/{id}`,
`/scenarios/{id}/locations[/new|/edit/{locationId}]`, same shape for `routes`/`shops`).

`ExplorableProgressIds`/`RequiredProgressIds` get checkbox-group pickers sourced from the
scenario's own `Progress.ProgressIds` (a closed set), matching the ruleset editor's own
"cross-references get pickers, not free text" convention. Shop items pick from the
ruleset's `EquipmentItems` via the existing `RulesetContentService`, cross-service the
same way `MonsterForm` already cross-references ruleset weapons.

The `Locations` form deliberately does not expose `ExplorationMap` -- editing one location
never disturbs a sibling's map (verified: `SaveLocation_RenamingALocationLeavesASiblingLocationsExplorationMapUntouched`),
and saving a location that itself has a map leaves it byte-identical (verified via
`ScenarioNoOpSaveFormattingTests` against all three real scenario files, and via an actual
browser save that produced a one-line diff).

40 new/updated tests (`ScenarioContentServiceTests`, `ScenarioNoOpSaveFormattingTests`),
all passing; full solution build+test gate green (2307 tests, 3 skipped, unchanged);
Godot build unaffected (confirmed, not re-run headless since nothing Godot-facing
changed). Also verified live in a browser: add/edit/delete for all three content kinds,
including the `Shops` property-insert path on a file that had none, and a rejected save
(empty origin/destination picker) correctly leaving the file untouched.

## Forward roadmap (not built yet)

- **Phase 2 -- Decisions + Triggers.** `Options` (variable-length, 2-3 seen in real
  content). `Trigger`'s independently-optional `Floor`/`Position`/`RequiredFacing`/
  `EncounterId` (`EncounterId` picker sourced from the scenario's own `Encounters` --
  decide whether this phase should follow Phase 3 or ship first with a type-the-id
  caveat, when it starts).
- **Phase 3 -- Encounters.** `Combatants` (`MonsterId` picker cross-referencing the
  ruleset bestiary), `PartyStartingPositions`/`BlockedPositions` (lists of
  `GridPosition`), `Outcome` (`VictoryProgressId`/`DefeatProgressId` pickers sourced from
  the scenario's own `Progress.ProgressIds`).
- **Phase 4 -- `ExplorationMap` editing** (`Floors`/`TraversablePositions`/`Stairs`/
  `Doors`/`Treasures`/`Npcs`). **The UX decision is made: visual click-to-toggle grid,
  not a raw X/Y list** (the user's own call, 2026-08-06 -- it's the "map maker" the
  original authoring-tool ask was actually about). Delivered in three slices:
  - **Phase 4a -- foundation. Done, 2026-08-06.** See the section below.
  - **Phase 4b -- the grid component** for toggling traversable cells per floor.
  - **Phase 4c -- feature layers** (stairs, edge-anchored doors, treasure, NPCs).
- **Phase 5 -- Campaign roster editor.** Separate `CampaignContentService`/
  `CampaignPackDocument`, roster CRUD (`AbilityScores` dict, `SelectedSkillIds`,
  `EquippedWeaponIds`, `Ammunition`, `PreparedSpellIds`), cross-referencing ruleset
  weapons/spells the same way Monster's weapon picker already does. `RaceId`/`ClassId`/
  `BackgroundId` stay free-text unless/until those become editable ruleset content
  themselves (they aren't today).

## Phase 4a -- done, 2026-08-06: the ExplorationMap renderer and save path

The write half of map editing, with no UI yet -- deliberately sliced so the byte-exact
formatting could be proven against real content before any grid component depends on it.

- **`ScenarioJsonFormatting.RenderExplorationMap`** renders the full nested shape
  (`Floors`/`TraversablePositions`/`Stairs`/`Doors`/`Treasures`/`Npcs`) on the existing
  `JsonLayoutPrimitives`. Two orderings in committed content are deliberately *not* the
  DTO's own and would have been silent corruption if assumed: a treasure writes
  `GoldPieces` **before** `ItemId` (`TreasureDefinitionV1` declares `ItemId` first), and
  a floor's `Stairs` is required-and-empty-rendered (`[]`, per Sunken Chapel) while
  `Doors`/`Treasures`/`Npcs` are omitted entirely when empty. Both were found by reading
  real content, not inferred from the types.
- **Design decision #4 is now split rather than deleted.** A location whose map isn't
  being edited still copies its original bytes verbatim; only the edited map is
  re-rendered. This is what keeps Phase 1's existing byte-identity tests passing
  untouched, and it means editing one location can never reformat a sibling's map.
- **The Hollow Mill ordering conflict is resolved by normalizing, not by preserving.**
  `RenderExplorationMap` writes one canonical field order (the DTO's, matching Watchtower
  and Sunken Chapel exactly). Editing a Hollow Mill floor also reorders its
  `Npcs`/`Doors`/`Treasures` block -- accepted, since that file is being rewritten anyway,
  and carrying each floor's original key order through a form round trip purely to
  preserve an inconsistency isn't worth the machinery.
- **`ScenarioContentService.FindExplorationMap`/`SaveExplorationMap`** follow the
  established pattern, so all 40 `scenario.map.*` validator rules already gate every save
  through the existing write-temp/validate/commit path -- the editor gets map correctness
  for free rather than reimplementing it.
- **6 new tests (40 -> 46).** The load-bearing one is byte-identity: re-rendering every
  map in Watchtower and Sunken Chapel reproduces them exactly, covering multi-floor and
  single-floor maps, an empty `Stairs` array, secret and locked doors, a treasure with
  gold plus item plus quantity, and an NPC. Hollow Mill gets an explicit
  content-equality-despite-reordering test rather than being quietly excluded. Plus:
  adding a cell persists, editing one map leaves others' bytes untouched, and an
  out-of-bounds cell is rejected with the file left byte-identical.
- Full solution gate green (Debug+Release 0 warnings; 2336 tests, 3 skipped, 0 failures).
  No public API change -- everything added is internal.

**Not done in 4a:** the form model. Deferred to 4b on purpose, so its shape is driven by
what the grid component actually needs rather than guessed ahead of it.

## Status (2026-08-06)

Phase 1 and Phase 4a are done and merged. Phase 4b (grid component) is next. Phases 2,
3, 4c and 5 are scoped above but not started -- pick up whichever is next via the
standing branch/PR/merge workflow, same as every other feature in this repo.
