# Data-driven content plan (campaign / scenario / ruleset content externalization)

## Motivation

CLAUDE.md has recorded since 2026-07-28 that the user's long-term goal is for adventures to
eventually be pluggable data — a file a loader reads — not compiled C#, "once the main driver is
stable," with building that loader explicitly deferred until then. The backend is now stable
(Priority 1 closed, 2105 tests, three working scenarios), and the user has asked for two things
concretely:

1. All campaign/scenario/adventure content should live outside `Core`/`Application`/etc., in a
   dedicated `/data/` folder — no hardcoded content of that kind in the engine.
2. Spell/weapon/item (and related ruleset) content should be stored the same way, loadable in
   packs, so new content can be added without recompiling.

This document is the phased plan for doing that, following a format/schema discussion recorded
below.

## Current state (confirmed 2026-07-30 by direct code inspection)

Nothing is externalized today. There is no `/data/` folder, no JSON/YAML schema for authored
content, and no loader for it — the only `System.Text.Json` usage anywhere in the repo is
`Persistence/ManualSaveSerializer.cs`, which serializes session *save state*, not authored
content. All campaign/scenario/ruleset content is hardcoded C# object-literal construction inside
`FiveEGoldBox.Application`:

| File | Lines | What it hardcodes |
|---|---|---|
| `Scenarios/CampaignRulesetContent.cs` + `.Spells.cs`/`.Features.cs`/`.Ids.cs` | 842 | Classes, races, skills, weapons, armor, equipment, spells, effects |
| `Scenarios/HollowMillScenarioDefinitionProvider.cs` | 529 | The Hollow Mill adventure |
| `Scenarios/SunkenChapelScenarioDefinitionProvider.cs` | 345 | The Sunken Chapel adventure |
| `Scenarios/WatchtowerScenarioDefinitionProvider.cs` | 328 | The Watchtower adventure |
| `Campaigns/FrontierCampaignContent.cs` | 307 | The party roster |

~3,400 lines total, all literal data wearing C# syntax.

The architecture is nonetheless already well-suited to externalization:

- **The data model is already pure data.** No delegates/`Func<>`/behavior were found anywhere in
  the authored-content record types under `Core/Definitions/` or
  `Application/Scenarios/Definitions/` — every one is a plain `sealed record` with
  `IReadOnlyList<T>` properties.
- **Content already flows through ID-keyed, validate-then-cache registries that don't care where
  the object graph came from:** `ScenarioDefinitionRegistry`, `RulesetRegistry`, and
  `CampaignRegistry` are each a `Dictionary<string, Func<T>>` resolved once, validated via the
  existing `ScenarioDefinitionValidator` / `ValidatedRuleset.Load` / `CampaignDefinitionValidator`,
  and cached. Swapping the `Func<T>` factory for "deserialize this file" is a localized change —
  nothing downstream (rules, Console, Godot) touches these providers directly.
- **A stricter-than-validation loading pattern already exists to extend:**
  `Randomness/ApplicationRulesetLoader.cs` already does "Core says this is well-formed, but can
  Application actually execute it" as a second pass after Core's own validation.
- **A versioned-DTO-plus-mapper pattern already exists for exactly this class of problem:**
  `Persistence/V1/` holds `internal sealed record` DTOs (`SaveGameV1` and its siblings) with a
  `FormatVersion` int field, translated to/from runtime types by a dedicated `SaveGameMapper`.
  This is the save-game system's answer to "the wire format must survive internal refactors,"
  and it's the pattern this plan reuses for content.
- **`ScenarioDefinition` and `CampaignDefinition` are `internal` to `Application`** (deliberately,
  from the Phase 12 API-shrinking work), while `RulesetDefinition`/`WeaponDefinition`/
  `SpellDefinition`/`EquipmentItemDefinition` in Core are already `public`. Mirroring the DTO
  pattern above sidesteps this entirely — the DTOs live inside `Application` regardless, same as
  `SaveGameV1` does today.
- **The existing ID convention is `type.name`** (`weapon.longsword`, `class.fighter`,
  `feature.sneak_attack`) — namespaced by kind of thing, not by owner. Nothing today stops two
  independent sources from both wanting to define `weapon.flametongue`, because nothing has had to
  survive multiple independent content authors before.

## Decisions locked in (this conversation, 2026-07-30)

1. **Format: JSON.** No new dependency, matches the existing save-game precedent.
2. **Schema approach: versioned DTOs + mapper**, mirroring `SaveGameV1`/`SaveGameMapper` exactly —
   not direct deserialization into the internal engine records.
3. **ID namespacing: keep the existing flat `type.name` convention.** The loader's pack-merge step
   fails loudly on any ID collision across loaded packs rather than silently letting one win.
4. **Scenario/campaign granularity: one file/tree per adventure, no multi-file pack merging.** A
   scenario is authored as one cohesive thing; pack-style multi-file merging is reserved for
   ruleset content (spells/weapons/items/classes/races), which is the actual "add content at will"
   use case described.

## Still open — to resolve during implementation, not blocking this plan

- **Where `/data/` lives on disk and how each runtime client finds it at startup.** A repo-root
  `/data/` directory is the obvious default, but Console and Godot are separate executables with
  separate working-directory conventions, and Godot in particular has its own `res://`/`user://`
  sandboxing that needs checking against the actual export pipeline before assuming it can read an
  external filesystem path the way `RealGameSession` already does for everything else. Scoped to
  Phase 4 below, deliberately last because nothing about it blocks the loader work itself — Phases
  1–3 are verified with test-local paths.
- **Exact on-disk directory tree layout within `/data/`** — file names, subfolder structure per
  scenario/pack. Left to be settled while writing Phase 1/2's DTOs, not decided in the abstract
  here.
- **Whether a hardcoded provider is deleted immediately on its phase completing**, or kept
  briefly as a reference during migration. Default plan below is "delete once the equivalence
  test passes," matching this codebase's general discomfort with keeping dead/parallel
  implementations around.

## Phased plan

### Phase 1 — Ruleset content pack loader (spells/weapons/items/classes/races/backgrounds/skills/armor/equipment/effects/features)

Why first: this is the explicit "add spells/weapons/items at will, in packs" ask; the constituent
types are already `public` in Core, unlike the `internal` scenario/campaign types, so there's less
friction; and it's the smaller schema, making it the right pilot for the DTO + mapper + pack-merge
pattern before applying it to the larger scenario schema.

1.1. Design versioned DTOs for `RulesetDefinition` and each nested type (`RulesetPackV1`,
     `WeaponDefinitionV1`, `SpellDefinitionV1`, etc.), `internal` to `Application`, with a
     `FormatVersion` field — same shape as `Persistence/V1/`.
1.2. Build a `RulesetPackMapper`: DTO → internal `RulesetDefinition`.
1.3. Build the pack-merge step: load a base ruleset pack plus zero or more additional pack files;
     union each content list (`Races`, `Classes`, `Weapons`, `Spells`, etc.) across all loaded
     packs; before handing the merged result to `ValidatedRuleset.Load`/
     `ApplicationRulesetLoader.Load`, check for duplicate IDs *within a type* across packs and
     fail loudly with a new validation issue code (e.g. `content.pack.duplicate_id`).
1.4. Author one real JSON pack file reproducing `CampaignRulesetContent`'s current output exactly
     (the base "core" ruleset pack).
1.5. **Verification:** a test that loads the JSON pack through the new loader and structurally
     compares the result against `CampaignRulesetContent.CreateRulesetDefinition()`'s live output.
     Note: this needs a real recursive/structural equality check, not `Assert.Equal` — C# records
     with `IReadOnlyList<T>` properties backed by arrays/lists don't get free deep equality from
     the generated `Equals`, since list types don't override structural equality. Green here is
     the proof the migration is byte-for-byte equivalent — the same discipline the frozen combat
     transcripts already use elsewhere in this codebase.
1.6. Point `RulesetRegistry`'s factory at the new file-based loader instead of
     `CampaignRulesetContent.CreateRulesetDefinition`, once 1.5 passes.
1.7. Delete `CampaignRulesetContent.cs` and its partials once nothing references them.

### Phase 2 — Scenario content loader (one file/tree per adventure, no merge semantics)

2.1. Design versioned DTOs mirroring the ~two dozen record types under
     `Scenarios/Definitions/` (`ScenarioPackV1`, `EncounterDefinitionV1`,
     `ScenarioTriggerDefinitionV1`, etc.).
2.2. Build a `ScenarioPackMapper`: DTO → internal `ScenarioDefinition`.
2.3. Author three JSON files/trees reproducing Watchtower, Sunken Chapel, and Hollow Mill exactly.
2.4. **Verification:** same structural-equality-against-live-provider-output discipline as
     Phase 1.
2.5. Point `ScenarioDefinitionRegistry`'s factories at the file-based loader.
2.6. Delete the three `*ScenarioDefinitionProvider.cs` classes once verified and unreferenced.

### Phase 3 — Campaign content loader

Same shape as Phase 2, applied to `CampaignDefinition`/`FrontierCampaignContent` (smaller — 307
lines, one file, no merge semantics).

### Phase 4 — `/data/` directory convention and runtime discovery

- ~~Proposed default: a repo-root `/data/` directory~~ — **done, as part of Phases 1–3.** All
  three content types live under a repo-root `data/` (`data/rulesets/campaign/core.json`,
  `data/scenarios/*/scenario.json`, `data/campaigns/frontier/campaign.json`), and
  `DataDirectoryLocator.ResolveDataFilePath` is the one seam every loader goes through.
- **Explicit override added (2026-07-30):** `DataDirectoryLocator` now checks a
  `FIVEEGOLDBOX_DATA_ROOT` environment variable first. When set, it is trusted absolutely — a
  missing file under it is a real misconfiguration and fails loudly rather than silently falling
  through to the search below, matching this codebase's existing "fail loudly, don't paper over a
  real conflict" discipline. Without it, the existing dev-checkout walk-up (from the running
  assembly's location up to a `data/` directory) is the fallback, unchanged.
- **Deliberately not added: a `--data-path` CLI argument for Console.** The environment variable
  already covers the override need end to end, and Console's `Program.Main` takes no arguments
  today — adding argument parsing for a need the environment variable already satisfies would be
  exactly the kind of speculative flag this codebase's own discipline argues against. Easy to add
  later (just set the environment variable from `Main` before the first resolve) if a real need
  surfaces.
- **Still genuinely open, not resolved here:**
  - Neither client has an actual publish/export pipeline yet — Console is run via `dotnet run`
    from a checkout (confirmed: no `dotnet publish` step exists in the README or CI), and Godot has
    no `export_presets.cfg` configured. So "how does a shipped build locate `/data/`" has an
    *override mechanism* now, but no actual packaging convention to set that override *to* — that
    packaging work doesn't exist yet for either client and is out of scope here.
  - **Godot's export-specific behavior is unverified, not assumed solved.** This sandbox has no
    Godot binary installed, so the original concern this phase raised — whether an *exported*
    Godot build can read an arbitrary filesystem path the way a plain .NET console app can, and
    whether `FIVEEGOLDBOX_DATA_ROOT` is inherited the same way in Godot's embedded-runtime process
    model — was not and could not be tested here. `dotnet build
    src/FiveEGoldBox.Godot/FiveEGoldBox.Godot.sln` (compile-only) is green, which confirms nothing
    broke, but is not evidence about export-time behavior. Whoever next has access to the Godot
    editor/export templates should verify this before relying on it for a real export.

### Phase 5 — Content-pack authoring ergonomics (future, not scoped in detail here)

- A JSON Schema file (or files) for editor autocomplete/validation while hand-authoring packs.
- A standalone validation command ("does this pack load and validate cleanly") independent of
  running the whole game.
- Not blocking — flagged so it isn't forgotten once the core mechanism exists.

## Sequencing rationale

Ruleset content (Phase 1) proves the harder pattern — multi-pack merge plus collision detection —
on the smaller, already-public schema. Scenario and campaign content (Phases 2–3) then reuse the
proven DTO + mapper approach without needing merge logic at all, since they were decided to stay
one-file-per-adventure. `/data/` location and runtime discovery (Phase 4) is deliberately last
because nothing about it blocks, or is blocked by, the loader work itself.

## Out of scope for this plan

- The save-game format (`Persistence/V1/`) itself — unrelated, already solved; cited here only as
  the precedent this plan reuses.
- Leveling/multiclassing content, condition-immunity enforcement, and other product-backlog items
  already tracked in CLAUDE.md — this plan is scoped strictly to *where authored content lives*,
  not *what mechanical content exists*.
