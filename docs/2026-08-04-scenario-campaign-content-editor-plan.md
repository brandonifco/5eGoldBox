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
  `Doors`/`Treasures`/`Npcs`). An open UX decision, not pre-solved: a raw add/remove list
  of X/Y fields is real but painful for 15-20+ cell floors (Hollow Mill's ground floor has
  17). A small interactive click-to-toggle grid component (SVG or CSS-grid-based) matches
  the "map maker" spirit of the original authoring-tool ask far better. Decide
  simple-list-v1 vs. visual-grid-v2 when this phase starts. This phase can finally delete
  the raw-byte-preservation mechanism from Phase 1's design decision #4, once editing
  (not just preserving) is real -- or keep it as the fallback for untouched locations.
- **Phase 5 -- Campaign roster editor.** Separate `CampaignContentService`/
  `CampaignPackDocument`, roster CRUD (`AbilityScores` dict, `SelectedSkillIds`,
  `EquippedWeaponIds`, `Ammunition`, `PreparedSpellIds`), cross-referencing ruleset
  weapons/spells the same way Monster's weapon picker already does. `RaceId`/`ClassId`/
  `BackgroundId` stay free-text unless/until those become editable ruleset content
  themselves (they aren't today).

## Status (2026-08-04)

Phase 1 is done and merged. Phases 2-5 are scoped above but not started -- pick up
whichever is next via the standing branch/PR/merge workflow, same as every other
feature in this repo.
