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
  - **Phase 4b -- the grid component. Done, 2026-08-06.** See the section below.
  - **Phase 4c -- feature layers. Done, 2026-08-06.** See the section below.
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

## Phase 4b -- done, 2026-08-06: the visual grid editor

`/scenarios/{id}/locations/{locationId}/map` -- a click-to-toggle grid, one tab per
floor, reached from an "Edit map" button on the location list (shown only for locations
that have a map). The banner on `LocationForm` that said maps weren't editable now links
here instead.

- **`ExplorationMapFormModel`/`ExplorationFloorFormModel`** (internal, same reason every
  scenario form model is). **`TraversablePositions` is an ordered `List`, deliberately
  not a `HashSet`** -- committed cell order is hand-authored and not purely row-major
  (Watchtower's ground floor ends with two cells clearly appended after the fact), so an
  unordered set would have silently rewritten every untouched floor's cell order on the
  first save. Toggling mutates in place: removing leaves the rest in order, adding
  appends. `RoundTrippingAMapThroughTheFormModelProducesAByteIdenticalFile` is the guard,
  and it covers what the 4a tests structurally could not, since those went through the
  DTO directly rather than the form model the UI actually uses.
- **Stairs/doors/treasure/NPCs round-trip untouched** through the floor model rather than
  being dropped -- 4b only edits walkable cells, but saving a floor must never silently
  destroy the features authored on it (`ToggleTraversable_LeavesStairsDoorsTreasuresAndNpcsIntact`).
- **They also render as read-only markers**, which was worth doing now rather than
  deferring wholesale to 4c: without them an author toggling cells has no idea what's
  already on the map. Start/stairs/treasure/NPC are glyphs; **a door renders as a thick
  border on the cell edge it actually sits on** (dashed when secret), matching the
  edge-anchored `Position` + `Side` model rather than pretending a door occupies a tile.
  Every cell carries a full tooltip (coordinates, walkability, and each feature's id).
- **Verified live in a browser**, not just by build+test -- the repo's standing convention
  for UI work. Confirmed: markers land exactly where the JSON puts them (14 of 18 walkable,
  ★ at the real starting position, ↕/$/@ on their real cells); floor tabs switch to
  UpperFloor's own 6 cells; toggling a cell and saving produced a **two-line diff on the
  real committed file** (one cell appended, every existing cell and all formatting
  untouched) which was then reverted; and making the NPC's own tile walkable is rejected
  on save with `scenario.map.npc_position_already_traversable` shown in the page's
  validation alert, leaving the file byte-identical.
- 5 new tests (46 -> 51). Full solution gate green (Debug+Release 0 warnings; 2341 tests,
  3 skipped, 0 failures). No public API change.

**Known limitation, deliberate:** editing Width/Height doesn't prune cells that fall
outside the new bounds -- the validator rejects the save with
`scenario.map.position_out_of_bounds` and the author removes them by hand. Auto-pruning
would silently delete authored content, which is worse than a clear rejection; a real fix
belongs with 4c's editing tools, not here.

## Phase 4c -- done, 2026-08-06: editable feature layers

The markers 4b rendered read-only are now placeable and editable, which closes out map
authoring: a floor's walkable cells, stairs, doors, treasure and NPCs are all editable
from the one grid.

- **Two modes on one grid, not two pages.** "Paint walkable cells" keeps 4b's click-to-
  toggle (a bulk operation that would be miserable behind a select-then-act step);
  "Edit cell features" makes a click select a cell and open a panel for everything on it.
  **A door is the reason a panel exists at all** -- it's edge-anchored (`Position` +
  `Side`), so a cell click alone can't express which of the four edges it belongs to.
  The panel gives one row per side.
- **New mutable form models per feature type** (`StairFormModel`, `DoorFormModel`,
  `TreasureFormModel`, `NpcFormModel`) because the V1 DTOs are records with init-only
  properties that Blazor's binding cannot write to.
- **Empty means absent, deliberately.** A treasure with no `ItemId`/`Quantity` must write
  those properties *not at all*, not as `""`/`0`. `TreasureFormModel.ToDefinition`
  normalizes blank to null and `ParseOptionalInt` keeps an empty number box null.
- **New feature ids are suggested, not required**, following committed content's own
  `kind.scenario-slug.name` convention and checked for collisions across *every* floor,
  since the validator's id rules are map-wide.

### The ordering bug this surfaced, and the content normalization that fixed it

Extending the byte-identity round-trip test to hollow-mill **failed immediately**, and
the failure was real rather than a bad assertion: committed content disagreed on a
floor's property order (Hollow Mill wrote `Npcs` before `Doors`; Watchtower the reverse),
while the 4a renderer writes one canonical order. 4a had *documented* this as an accepted
trade-off ("editing a Hollow Mill floor also normalizes its ordering"), but 4c is what
makes maps genuinely editable, which turned it from a footnote into a live footgun: the
first person to edit the richest scenario's map would get a large spurious block move
mixed into their real diff.

Fixed by normalizing the committed files once through the renderer itself, via a new
skipped-by-default `CommittedScenarioMapNormalizer` (the same un-skip/run/re-skip
convention as `FixtureWriter`/`ContentPackSchemaWriter`). Only hollow-mill changed, by
8 moved lines; all three scenarios were verified to parse to structurally identical
content before and after, and the full Application suite (which covers Hollow Mill's
scenario behaviour) passes unchanged. **A no-op save is now byte-identical for every
scenario**, which the test asserts for all three -- so reintroducing a divergent hand
ordering now fails a test rather than silently producing noisy diffs.

- 6 new tests (51 -> 57, plus the skipped writer). Full solution gate green
  (Debug+Release 0 warnings; 2347 passed, 4 skipped, 0 failures). No public API change.
- **Verified live in a browser:** the mode toggle defaults to painting; selecting
  Garrick's cell loads his real id/name/dialogue; adding a treasure auto-suggests
  `treasure.hollow-mill.new-1` and updates the grid marker and tooltip immediately; and a
  real save wrote a gold-only treasure with **no `ItemId`/`Quantity` properties at all**
  alongside an edited NPC name, formatted identically to its hand-authored neighbours.
  Test edits were reverted afterward.

**One environment trap worth remembering:** the Phase 4b dev server survived `TaskStop`,
so the next `dotnet run` failed to bind while `curl` still answered on the port -- which
looks exactly like "the server is up" while actually serving the *previous* build. Kill
stale `dotnet run` processes and wait for the new process's own "Now listening on" line
rather than probing the port, or you will verify stale code.

## Status (2026-08-06)

Phase 1 and all of Phase 4 (a/b/c) are done and merged -- **exploration maps are now
fully authorable in the editor**, which was the largest remaining gap in the adventure
builder. Phases 2 (Decisions+Triggers), 3 (Encounters) and 5 (campaign roster) are scoped
above but not started -- pick up whichever is next via the standing branch/PR/merge
workflow, same as every other feature in this repo.
