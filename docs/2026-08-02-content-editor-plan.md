# Content editor plan (Phase F of the adventure authoring tool)

## Motivation

`docs/2026-07-30-adventure-authoring-tool-plan.md`'s Phase F calls for a form-based tool for
authoring weapons/spells/items/monsters, so ruleset content stops being hand-edited JSON. That
plan deliberately left the tool's shape as "likely a minimal ASP.NET Core web app" — a direction,
not a locked decision. This doc locks in the remaining decisions and records the concrete plan.

## Decisions locked in (2026-08-02, via user confirmation)

1. **UI: Blazor Server**, a new project (`src/FiveEGoldBox.ContentEditor`), not Razor Pages/MVC and
   not a Console-based wizard. Forms are C# components with direct in-process calls into
   `FiveEGoldBox.Application`/`Core` — no separate JS layer or REST API to invent. Run with
   `dotnet run --project src/FiveEGoldBox.ContentEditor`, opened in a browser; local-only dev tool,
   no auth, no deployment story.
2. **All four content kinds now** — weapons, spells, items, monsters — not staged behind a
   simpler weapons/items-first cut. The nested-list editing pattern (spell effects, monster ability
   modifiers/weapons) only needs solving once; solving it for all four together means the flat
   forms (weapons, items) and the nested ones (spells, monsters) share one design pass.
3. **Full CRUD** — create, edit, and delete existing entries, not create-only. A tool that can't
   fix a typo in an existing weapon doesn't really replace hand-authoring, which is what this
   phase exists to do.

## How it writes content (not asked, but a real decision — recorded so it isn't silently assumed)

**Raw JSON-node manipulation, mirroring Phase E's Tiled importer exactly** — not a round-trip
through the internal `RulesetPackV1`/`RulesetDefinition` DTOs. Phase E already established the
precedent for authoring tools that need to write ruleset/scenario content without reaching into
`Application`'s internals ("this works purely at the plain-JSON level
(`System.Text.Json.Nodes`), never touching the internal pack types" — `TiledMapConverter.cs`).
The content editor reads `data/rulesets/campaign/core.json` as a `JsonObject`, the Blazor forms
own their own plain view-model types (not `Application`'s internal DTOs), and a save mutates only
the relevant top-level array (`Weapons`, `Spells`, `EquipmentItems`, `Monsters`) before writing the
whole document back out. This needs no `InternalsVisibleTo` grant and keeps the editor decoupled
from `Application`'s internal DTO shape.

**Validation after every write, before the file is considered saved**, through the existing
public `ContentPackValidation.ValidateRulesetPack(IReadOnlyList<string> filePaths)` — the same
facade the standalone `validate` command already uses. A save that fails validation shows the
issues and does not commit the write (matching "loading refuses, tooling reports" — here, saving
refuses).

**Single target file for v1**: `data/rulesets/campaign/core.json` is the only ruleset pack file
that exists today, so the editor targets it directly rather than generalizing to
`RulesetPackLoader.Load(IReadOnlyList<string>)`'s multi-pack-merge case up front. Nothing about
the JSON-node approach blocks generalizing later if a second pack file is ever authored.

**Cross-references get pickers, not free text.** `MonsterWeaponDefinition.WeaponId` (against
`Weapons`) and `SpellDefinition.AppliedEffectId` (against `Effects`) are dropdowns populated from
the file's own current content, not text inputs — a typo'd reference is exactly the kind of
mistake `scenario.combatants.monster_id_unresolved`-style validation exists to catch, and a picker
prevents it at entry time instead of at validation time.

**Round-trip formatting is a real risk to verify, not assume.** Re-serializing the whole document
after an edit could reformat parts `System.Text.Json` didn't touch (indentation, property order)
even if it doesn't change their values — the implementation needs to confirm a save that changes
nothing produces a byte-identical file (or a deliberately-accepted, understood diff), the same
diligence Phase E's own idempotency section already applied to its own writes.

**Project wiring**: added to `5eGoldBox.sln` (small ASP.NET Core project, fast to build, no reason
to keep it out of the normal build/test gate the way Godot's separate engine dependency justifies
keeping that `.sln` apart).

## Findings — the four content shapes, as they exist today

- **`WeaponDefinition`** (`Core/Definitions/WeaponDefinition.cs`) — flat: `Id`, `Name`, `Category`
  (enum), `AttackKind` (enum), `Damage` (`DamageDice { Count, Die }`), optional
  `VersatileDamage`, `DamageType` (string), `Properties` (string list, drawn from a small known
  set — `weapon_property.*`), optional `ReachFeet`/`NormalRangeFeet`/`LongRangeFeet`, optional
  `AmmunitionItemId` (references `EquipmentItemDefinition.Id`), `WeightPounds`, optional
  `CostInCopperPieces`. No nested list of sub-objects — the only list is `Properties`, a flat
  string list. Simplest of the four.
- **`EquipmentItemDefinition`** (`Core/Definitions/EquipmentItemDefinition.cs`) — flat: `Id`,
  `Name`, `WeightPounds`, optional `CostInCopperPieces`, `Tags` (flat string list). Simplest
  overall; only one item exists in content today (`item.arrow`).
- **`SpellDefinition`** (`Core/Definitions/SpellDefinition.cs`) — has real nested structure:
  `Effects` is a list of `SpellEffectDefinition` (`Kind`, `Dice`, `Instances`,
  `AddsSpellcastingModifier`, optional `DamageType`), plus scalar fields (`Cost`, `Level`,
  `CastingTime`, `RangeKind`, optional `RangeFeet`, `MaximumTargets`, `Targets`, `Resolution`,
  optional `SaveAbility`/`SaveOutcome`, optional `AppliedEffectId` — cross-references `Effects`
  top-level list by ID —, `RequiresConcentration`, optional `DurationRounds`). The
  `AppliedEffectId` cross-reference and the `SpellResolutionKind`-dependent fields (`SaveAbility`
  only matters for `SavingThrow` resolution) are the two places the form needs conditional
  fields/validation feedback, not just flat inputs.
- **`MonsterDefinition`** (`Core/Definitions/MonsterDefinition.cs`, landed in Phase C/PR #203) —
  also nested: `AbilityModifiers` (list of `MonsterAbilityModifier { Ability, Modifier }`, exactly
  six required — one per `Ability` enum value, per `RulesetValidatorMonsterDefinitions.cs`'s
  existing completeness check) and `Weapons` (list of `MonsterWeaponDefinition { WeaponId,
  AmmunitionItemId?, AmmunitionQuantity? }` — cross-references `Weapons` top-level list by ID),
  plus scalars (`MaximumHitPoints`, `ArmorClass`, `MovementSpeedFeet`, `ZeroHitPointPolicy`,
  `ProficiencyBonus`, `IsProficientWithWeapons`). The ability-modifier list is naturally a
  fixed-six-row form (one row per `Ability`, not an add/remove list), unlike `Weapons`, which is a
  real add/remove list.
- **Current content volume**: `data/rulesets/campaign/core.json` today has 11 weapons, 6 monsters,
  1 equipment item, 6 spells, 1 effect — small enough that manual verification of every form
  (create, edit, delete, round-trip) against the real file is realistic before merging.

## Phased plan

### Phase F.1 — Project scaffold, read-only list views, validation wiring

New `src/FiveEGoldBox.ContentEditor` Blazor Server project, referencing `FiveEGoldBox.Application`
(for `ContentPackValidation`) — added to `5eGoldBox.sln`. A landing page lists all four content
kinds read from `data/rulesets/campaign/core.json`, read-only. This phase proves the JSON-read +
`ContentPackValidation` wiring and the project's basic shape before any write path exists.

### Phase F.2 — Weapon and item forms (flat shapes)

Create/edit/delete forms for `WeaponDefinition`/`EquipmentItemDefinition`, the two flat shapes —
proves the write-then-validate-then-commit save path (and the "does a no-op save stay
byte-identical" check) on the simplest content before tackling nested lists.

### Phase F.3 — Spell and monster forms (nested shapes)

Create/edit/delete forms for `SpellDefinition`/`MonsterDefinition`, including the nested
`Effects`/`AbilityModifiers`/`Weapons` sub-lists and the `WeaponId`/`AppliedEffectId`
cross-reference pickers described above.

### Phase F.4 — Polish pass

Delete-confirmation (a delete that breaks a cross-reference — e.g. deleting a weapon a monster
still carries — should be caught by the post-save validation call and reported, not silently
allowed), and a final pass confirming every content kind's create/edit/delete round-trips clean
against the real `core.json`.

## Out of scope for this plan

- Scenario/campaign content (locations, encounters, triggers, roster) — Phase F is ruleset-only,
  matching the original plan doc's own scope (`data/rulesets/**/*.json`). Editing scenario content
  is a different, unscoped tool.
- Multi-pack-file support (only `data/rulesets/campaign/core.json` exists today; see above).
- Tile/map authoring — that's Phase E (Tiled importer), already done.
- Any deployment/hosting/auth story — this is a local dev-only tool, same as the Console client.

## Status (2026-08-02, scoped, not started)

Nothing built yet. Phases F.1–F.4 above are the intended sequence; ask before picking one up if
priorities have shifted since this was written.
