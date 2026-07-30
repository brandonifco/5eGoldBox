# 5eGoldBox — Codebase Reevaluation & Development Plan

**Date:** 2026-07-30
**Method:** Four independent, parallel deep-reads of the codebase (Core; Application; Console+Godot clients; tests/tooling/docs), each instructed to verify claims against actual code rather than trust CLAUDE.md/README/inline comments at face value. Findings below are marked **[confirmed]** where the reviewing agent read the code, built it, ran it, or reflected on the compiled assembly themselves, and **[claim]** where something is asserted by a comment/doc and not independently re-checked here. File:line references are as of commit `176df5a` plus the uncommitted Area Map + movement work in the working tree at review time.

This document is a snapshot and a plan, not a permanent record — update or delete sections as they're closed out, the way `CLAUDE.md`'s own phase tracking already does.

---

## Executive summary

The codebase is in genuinely good shape at the foundation (Core) and reasonably good shape at the orchestration layer (Application) and Godot UI — the engineering discipline this team has documented (purity, deterministic randomness, reflection-verified public API counts, frozen characterization tests, `TreatWarningsAsErrors`) is **real, not aspirational**: independently re-verified and still holding under continued feature growth. The two biggest problems found are not bugs in the traditional sense — they're **documentation that no longer matches reality** (in one case, self-contradicting *within the same uncommitted edit*) and **a real, unmitigated CI gap** on the client that's absorbed the most recent development effort.

Top 10 findings, ranked by consequence:

1. **README.md is severely stale** — wrong party composition/size, wrong scenario count, claims the Godot UI is "an unwired shell" (it isn't), test count off by 18%+. Anyone onboarding from it gets a materially wrong picture.
2. **CLAUDE.md's own headline numbers are currently self-contradictory** — the top summary says "63 exported types, 2099 tests," but a paragraph further down in the *same uncommitted edit* documents bumping to 64/2105 and never updates the summary above it. This is the exact failure the doc's own "don't trust doc claims, reflect on the DLL" discipline exists to prevent, happening inside the document that states that discipline.
3. **`FiveEGoldBox.Godot` has zero CI coverage** despite 45 of the last 100 commits touching it. It builds clean today (verified), but nothing stops a silent regression on the next push — the only safety net is manual headless-boot + the user screenshotting/playing it themselves.
4. **A likely 5e rules-correctness deviation is pinned as "correct" by a passing test**: `DamageRules.ApplyDamageResponsesCore` halves-then-doubles when a target has both Resistance and Vulnerability to the same damage type (11 → 10 damage), where 5e RAW says the two cancel and full damage applies (11 → 11). This needs an explicit decision, not a silent fix.
5. **Six public `Core.Rules` classes are functionally dead** — unit-tested but never called from any real game path: `AbilityCheckRules`, `SkillCheckRules`, `AbilityContestRules`, `SkillContestRules`, `D20ContestRules`, `CombatRules`. Race-granted condition immunities are fully resolved onto every character and then never checked (`ConditionRules.CanApplyCondition` also has zero callers) — a maintainer could break any of these with no visible effect and CI would stay green.
6. **A collision-free hotkey-assignment algorithm is duplicated verbatim three times in Godot**, and the code says so out loud in comments at all three sites without anyone having consolidated it.
7. **Godot's `DirectMovement` interaction context is now overloaded for two different features** (front-facing movement, area-map movement) via two nullable session fields that must be nulled together at every exit point — works today with two consumers, will not scale cleanly to a third.
8. **Application's public API constrains its one real client in confirmed, concrete ways**: `RealCombatSession.cs` has three self-documented workarounds (re-deriving party-side membership, degraded enemy name display, string-parsing a UI label for a missing structured field) because internal Application types aren't reachable from Godot.
9. **The "only one session substate is populated" invariant is enforced by five separate, copy-pasted validator files** rather than once — a new nullable substate needs someone to remember to touch all five by hand, with nothing to catch a miss.
10. **No shared "turn a `CombatStepResult` into text" layer exists in Application** — Console and Godot each independently hand-rolled their own combat narration from scratch; a third client would do it a third time.

None of these are emergencies. Most are cheap. A few require a real decision from you before anyone should touch code. The plan below is ordered so cheap/high-leverage/decision-free work comes first.

---

## Phase 0 — Documentation truth pass (do first; cheap, zero risk, unblocks everything else)

Nothing here requires a design decision. This just makes the docs match reality again, which matters because two of the top-10 findings are *documentation itself being wrong in a way that actively misleads*.

- [ ] **Rewrite README.md.** It currently claims a 3-member party (Fighter/Barbarian/Ranger) — actual is 4 active (Fighter/Rogue/Cleric/Wizard, Barbarian/Ranger reserve), per `src/FiveEGoldBox.Application/Campaigns/FrontierCampaignContent.cs`. It claims one scenario and "not yet scenario-agnostic" — actual is three scenarios (Watchtower, Sunken Chapel, Hollow Mill), engine-agnostic since Phase 6/7. It claims the Godot UI is "an unwired shell... not yet wired to the engine" — actual is a real, backend-wired, playable client (outpost/exploration/travel/combat/spellcasting/area-map). It claims "~1,780 tests" — actual is 2105.
- [ ] **Fix CLAUDE.md's self-contradiction.** The top "Where things stand" summary line ("63 exported types... 2099 tests") needs to read 64/2105 to match the "Real Area exploration grid map wired" paragraph a few lines below it that already documents the bump. This is a one-line fix; do it as part of committing the current Area Map + movement work.
- [ ] **Strike through the two stale Phase-11 claims.** Two entries dated PR #170 ("combat... not wired," "combat integration... deliberately not started") are superseded by the same-day-later "Real combat wired (basic slice) — done" entry but were never annotated the way this doc's other ~24 historical corrections are (`~~old claim~~ — resolved (PR #x)`). Add the strikethrough + pointer.
- [ ] **Fix the stale "~22 validator rules" claim** in the Phase 6 narrative — actual distinct `scenario.*` rule codes in `ScenarioDefinitionValidator*.cs` today is 91 (grown legitimately as later validator files were added; the number just was never updated).
- [ ] **Consider whether CLAUDE.md needs restructuring, not just correcting.** It's ~8300 words — dense enough that a linear "read this first" pass is not a realistic expectation, which is plausibly *why* the self-contradiction in finding #2 went unnoticed. Options worth weighing (this one's a judgment call, flagged for you rather than just done): split into a short "current state" doc plus an archival "history" doc; or keep one file but move the phase-by-phase history to the bottom and lead with only what's true *today*. Not urgent, but the size is now working against the document's own stated purpose.

## Phase 1 — Rules-correctness decision (needs your call before any code changes)

- [ ] **Decide on Resistance+Vulnerability interaction.** `src/FiveEGoldBox.Core/Rules/DamageRules.cs`, `ApplyDamageResponsesCore` (~lines 241-263): currently halves then doubles (11 → 10 for the tested case), where 5e RAW says the two effects cancel and normal damage applies (11 → 11). This is pinned as intended by `DamageRulesTests.cs:110`. Three options: (a) it's a deliberate house rule — leave it, but say so explicitly in a comment so the next person doesn't "fix" it into a regression; (b) it's a bug — correct it to RAW cancellation and update the pinned test; (c) you want to check whether it currently matters in practice (does any current encounter/spell actually stack Resistance and Vulnerability on the same target?) before deciding. Low urgency mechanically (touches one function + one test), but it's a real behavior change either way and shouldn't happen without you weighing in.

## Phase 2 — Close the Godot CI gap (real risk, not just cleanliness)

- [ ] **Get `FiveEGoldBox.Godot.sln` into CI**, even minimally. It's excluded from `5eGoldBox.sln` and from `.github/workflows/dotnet.yml` entirely — confirmed, not claimed. It builds clean locally today (0 warnings), so this is about *keeping* it that way, not fixing something broken. Minimum viable version: a CI job that runs `dotnet build src/FiveEGoldBox.Godot/FiveEGoldBox.Godot.sln -c Debug` and the existing headless-boot check (`godot --headless --path src/FiveEGoldBox.Godot --quit-after 15`, expect exit 0 / empty stderr) on every push touching that tree. This would have caught, for free, any of the "one real bug found while screenshot-verifying" incidents CLAUDE.md already records happening by hand.
- [ ] **Longer-horizon, lower-priority:** a real automated interaction test (drive a scene, assert on rendered state) would be much more valuable than the build+boot smoke check, but there's no existing pattern for this in the repo and no GUI-automation tooling installed in the dev environment (confirmed while trying to do exactly this earlier in this session — no `xdotool`/screenshot tooling, and Godot isn't Electron/browser-based so existing driver patterns don't transfer). Worth a dedicated investigation later, not blocking Phase 2's minimum version.

## Phase 3 — Cheap, low-risk debt paydown (no design decisions needed, just do these)

- [ ] **Collapse the triplicated hotkey-assignment algorithm.** Identical letter-then-digit collision-avoidance logic exists at `RealGameSession.cs:334-359` (`AssignHotkey`), `ShellInteractionController.RealCombat.cs:398-423` (`AssignSpellHotkey`), and `ShellInteractionController.RealSession.cs:167-188` (`AssignAreaHotkey`) — each comment admits the duplication. Extract one `internal static class HotkeyAssigner` (e.g. under `ui/models/` or a new `ui/integration/` helper) taking a candidate-letter sequence and a `HashSet<char>`, and point all three call sites at it.
- [ ] **Add an `Enum.IsDefined` guard helper in Core.** The `if (!Enum.IsDefined(x)) throw new ArgumentOutOfRangeException(...)` shape appears 25 times verbatim-shaped across `DamageRules.cs`, `ConditionRules.cs`, `D20Rules.cs`, `AbilityRules.cs`, `EncounterSavingThrowRules.cs`, `RollContributionRules.cs`, and others. A single `internal static class CoreEnumValidation { RequireDefined<TEnum>(value, paramName, message) }`, parallel to the existing `Internal/CoreCollectionProtection.cs`, collapses this with no behavior change.
- [ ] **Centralize the "only one session substate populated" invariant.** `ApplicationSessionRules`'s five validators (`OutpostSessionValidator`, `TravelSessionValidator`, `ExplorationSessionValidator`, `EncounterSessionValidator`, `ScenarioConclusionValidator`) each independently repeat near-identical negative null-checks for the other substates. A single `RequireOnlyModeIsPopulated(state, allow: ...)` helper collapses ~20 lines of repetition per file and — more importantly — means a future sixth nullable substate can't be added without every validator being forced to acknowledge it.
- [ ] **Resolve the mock/real "Area" naming collision.** `ShellInteractionController.SecondaryScreens.cs`'s `ShowAreaMapScreen()` (a mock, body-text-only placeholder reachable only from the mock exploration command bar) and the new real `EnterAreaMapMode` (`.RealSession.cs`) now share the "Area Map" name and concept. Not broken today since they're reached from different sessions, but rename the mock one (e.g. "Area Map (mock)" or fold it into the real path's dev-shortcut story) before the next person conflates them.
- [ ] **Add a regeneration helper for the frozen combat transcripts**, matching the pattern `FixtureWriter.cs` already established for the JSON save fixtures (`[Fact(Skip = "Run by hand when a fixture is deliberately regenerated.")]`). Currently `tests/FiveEGoldBox.Application.Tests/Fixtures/watchtower-combat-{aggressive,passive}-transcript.txt` have no equivalent — someone would have to manually adapt the assertion to a file-write to regenerate one today.
- [ ] **Consider making `SpellSlotResources` (`FiveEGoldBox.Application.Parties`) internal.** Public today, but every real caller is inside Application itself (`CampaignPartyFactory.cs`/`CampaignResourceGrants.cs`) plus tests, which already have `InternalsVisibleTo` access. Zero-cost API-surface trim if/when the public surface gets revisited.

## Phase 4 — Architectural decisions that need a call before implementation

These aren't "just fix it" items — each has a real fork in the road.

- [ ] **Decide the fate of the six dead `Core.Rules` classes** (`AbilityCheckRules`, `SkillCheckRules`, `AbilityContestRules`, `SkillContestRules`, `D20ContestRules`, `CombatRules`) and the inert condition-immunity subsystem (`ConditionRules`, `CharacterConditionImmunity`). Three real options: (a) wire them into `Runtime/` where content actually needs them (e.g. the first monster/trap that calls for a raw ability check, or the first condition-inflicting spell, would finally exercise this machinery) — this fits the project's own stated "mechanics are built when authored content needs them" philosophy, meaning the *building* already happened ahead of the content that was supposed to justify it; (b) make them `internal` until content needs them, since nothing external calls them today; (c) leave as-is if you'd rather keep the public surface ready for near-term content work. This one is really a scheduling question: is ability-check/condition content coming soon enough to justify keeping this public and unused in the meantime?
- [ ] **Decide whether to collapse the identical `Combat*`/`Watchtower*` type pairs.** The dual hierarchy is well-justified for types with real validation (`CombatDecision`, `CombatWeaponAttackStepDetail`), but several pairs are byte-identical with no validation difference (`CombatIntentKind`/`WatchtowerCombatIntentKind`, some intent records) — roughly 700-900 lines of `WatchtowerCombatResultMapper`/`CombatViewFactory`/`CombatOperations` wrapping exist partly to translate between representations that don't actually differ for those specific types. A full collapse of the *entire* hierarchy is real, risky refactoring (CLAUDE.md already flags the ~30 remaining `Watchtower*`-prefixed files as "large and purely cosmetic; not bundled" into past work) — but a narrower version (drop the wrapper only for the identical pairs, keep it for the validated ones) is a smaller, lower-risk slice worth scoping separately if you want to chip at this.
- [ ] **Design a real "movement mode" abstraction before a third feature wants to piggyback on it.** `DirectMovement` context currently works via `_activeRealMovementSession`/`_activeAreaMapSession`, two nullable fields that `EnterAreaMapMode`, `EnterRealMovementMode`, `ExitExplorationMovementMode`, and `ReportMovement` all have to coordinate by hand. It's fine at two consumers. The next "let me do X while moving" feature (a minimap overlay? a look-around mode?) would either duplicate the pattern a third time or force a real refactor — better to design that abstraction now, deliberately, than discover the need mid-feature a third time.
- [ ] **Consider a shared combat-narration layer in Application.** Console's `RenderWeaponAttackStep`/`RenderSpellAttackStep` (verbose field dumps, appropriate for a debug-grade reference client) and Godot's `RealCombatSession.DescribeWeaponAttack`/`DescribeSpellAttack` (short player-facing sentences) solve the same problem — "turn a `CombatStepResult` into text" — independently. They render *differently* (verbose vs. narrative) on purpose, so this isn't a straight duplication-removal; it's worth deciding whether Application should own a shared low-level narration primitive (e.g. "what happened" as structured data both clients format differently) versus leaving each client's rendering fully independent, which is the status quo.

## Phase 5 — Loosen the Application/Godot API boundary where real friction exists

Three concrete, confirmed workarounds exist in `RealCombatSession.cs`/`RealGameSession.cs` because Application keeps something internal that Godot legitimately needs:

- [ ] **Expose party-side determination.** `RealCombatSession.cs:35-38`'s own comment says it re-derives the party-member-ID set itself because `EncounterPartySideResolver`/`PartySideId` (the real mechanism `CombatOperations.Query` already uses internally) isn't public. Either expose a narrow public accessor for this, or confirm the current re-derivation is provably equivalent and document why duplicating it is fine.
- [ ] **Give enemies a real display name.** `RealCombatSession.DescribeLabel` (~line 317-328) falls back to raw combatant IDs for enemies because `CombatantDefinition.DisplayName` lives on the internal `EncounterDefinition`. A small public projection (even just `CombatantView.DisplayName`, alongside the existing `CombatantId`) would fix combat-log readability for every enemy, in both clients, at once.
- [ ] **Give `SessionAction` a structured destination field.** `RealGameSession.BeginJourney` (~line 255-264) strips a hardcoded `"Set out for "` prefix off `DisplayName` to recover a destination location ID, because no structured field carries it. Add one (e.g. `SessionAction.DestinationLocationId`) and delete the string-parsing.

Each of these is small, additive, and (per this project's own discipline) should go through the same "public API count moved deliberately, verified by reflection" gate the rest of the public surface uses.

## Phase 6 — Known product backlog (unchanged, consolidated from CLAUDE.md's existing "what's next")

Not new findings — carried forward from CLAUDE.md's own current "What's next," included here so this document is a complete picture of open work, not just new complaints:

- [ ] Wire the M9 secondary screens (Character/Inventory/Spellbook) to real backend data — currently mock content.
- [ ] Bless's multi-target `TargetCombinations` (casting on 2+ allies in one cast) — single-target casting is wired, multi-target isn't.
- [ ] Player-facing death-saving-throw choices — proven at the backend, not wired to either client's UI.
- [ ] M10 (Standard/Immersive presentation equivalence), including the Immersive safe-zone overlap already flagged and deferred at M7g/M8g.
- [ ] Re-author the mock combat art for the isometric grid lattice (currently mismatched since the isometric re-render; real combat's plain placeholder is unaffected).
- [ ] Click-to-move / pan-zoom for the area map, if a floor turns out too large to read at a glance in practice.
- [ ] The user's stated long-term goal: adventures as pluggable external data rather than compiled C# — the Hollow Mill provider (Phase 4 audit finding) is a clean example of exactly what a future content loader would need to replace; worth revisiting once "the main driver" (per your own prior framing) is stable.

---

## Appendix — condensed findings by layer

### Core (`src/FiveEGoldBox.Core`, 219 files / ~14.4K LOC)

- **[confirmed]** Purity claim holds: zero `Random`/`DateTime.Now`/console/file I/O/mutable-static-state found anywhere in Core. One cosmetic exception: `Characters/CharacterResolver.cs:40,46` uses `Environment.NewLine` in an exception message (not rules-affecting).
- **[confirmed]** Module split is real, not just folder-naming: `Rules/` (72 files, stateless primitives) vs. `Runtime/` (69 files, `EncounterState`-threading orchestration built from `Rules/` primitives) — traced actual call chains, not just names.
- `Runtime/` is ~5x any sibling folder (6980 LOC / 69 files) — not currently a dumping ground, but the folder most likely to need subdivision if the game grows further before it's split.
- Zero interfaces anywhere in the whole solution; naming (`Rules`/`Resolver`/`Validator`/`Evaluate`) is consistently applied across all 188 public types.
- **[confirmed]** `CreateValidDraft`'s Phase-8 duplication-measurement claim (39 declarations / 24 distinct bodies) still holds exactly under independent re-derivation.
- Six public `Rules/` classes + `ConditionRules` are functionally dead in production (see Executive Summary #5 / Phase 4).
- Resistance+Vulnerability damage-response interaction likely deviates from 5e RAW (see Phase 1).
- 25x duplicated `Enum.IsDefined` guard blocks (see Phase 3).

### Application (`src/FiveEGoldBox.Application`, 191-197 files / ~15K LOC)

- **[confirmed]** The "`Watchtower*`-prefixed types are cosmetic naming debt, not behavioral coupling" claim holds — grepped all 33 internal `Watchtower*` files in `Combat/` for scenario-specific literals, zero hits.
- **[confirmed]** Public API is 64 exported types (reflected on the built DLL directly), not the 63 CLAUDE.md's top summary currently states — see Phase 0.
- Session-state invariant ("only one substate populated") is enforced but duplicated 5x (see Phase 3).
- Hollow Mill's scenario provider is 100% declarative with no behavioral escape hatches — strong confirmation the content model works (see Phase 6's content-as-data note).
- Persistence's `ActiveEncounter` save exclusion is real, consistent, and honestly documented — not a design flaw, a scoped-out feature.
- `CombatWeaponAttackStepDetail` and similar types expose `Core.Rules` types directly in Application's public surface; Godot depends on Core's types transitively with no `ProjectReference` to Core. "Client-agnostic public API" is true in spirit (no client-specific code in Application) but not Core-opaque in practice.
- Three concrete, confirmed API-boundary workarounds exist in Godot's real-session code because of this (see Phase 5).

### Console + Godot clients (Console: 6 files/~1.8K LOC; Godot: 132 files/~9.7K LOC)

- Console's non-combat code (`ConsoleSessionRunner.cs`/`.Noncombat.cs`) is clean and thin — genuinely just projects `SessionView.Describe`. Its combat code (`.Combat.cs`, 953 lines, over half the client) is a debug-grade full-field dump — legitimate for its purpose, not a template to imitate elsewhere.
- `ShellInteractionController` spans 7 files / 1,475 lines; `ShellPresentationController` spans 4 files / 461 lines. The partial-class split is well-motivated (each file states why it split off) but "what happens when combat ends" genuinely requires reading 3 files together — disciplined sprawl, still sprawl.
- Mock content (`ui/mocks/`, 1,805 LOC) is ~2.5x the real integration path (`ui/integration/`, 709 LOC) — a deliberate, working QA seam (`#if DEBUG` F-key shortcuts), not accidental, but "the measure to watch" as the real path keeps growing.
- **[confirmed]** View-model layering discipline is solid: grepped every Godot presentation/controller/component file for `using FiveEGoldBox.Application` — only the two `Real*`-integration files reference Application types at all; nothing else reaches past the Godot view-model boundary.
- Hotkey-assignment triplication and the `DirectMovement` overload are the two concrete, fixable findings from this quadrant (see Phases 3 and 4).

### Tests, tooling, docs (206 test files / ~65.4K LOC vs. ~41K LOC of source)

- **[confirmed]** `dotnet test 5eGoldBox.sln -c Debug`: 2105 total (2104 passed, 1 skipped), 0 failures, 0 build warnings — run directly, not assumed.
- Spot-checked largest test files (`EncounterWeaponAttackRulesTests.cs` 1663 lines/40 facts, `ConsoleProcessRestartTests.cs` 1364 lines/7 integration tests, `ManualSaveSerializerTests.cs` 1371 lines/55 tests) — size is earned by genuinely distinct scenarios or real out-of-process integration testing, not padding.
- Builder adoption (`TestRulesetBuilder`) is broad today (24 files), consistent with CLAUDE.md's account of Phase 8 fixing a prior stall.
- **[confirmed]** `TreatWarningsAsErrors`/`AnalysisLevel=latest` gate is real and unweakened — zero `NoWarn`/`#pragma warning`/`SuppressMessage`/editorconfig-severity-override hits anywhere in the tree.
- Frozen combat transcripts are a healthy characterization-testing pattern (legible diff format, explicit "this means behavior changed, not that the fixture needs updating" framing) but lack the regeneration tooling the JSON save fixtures have (see Phase 3).
- README and CLAUDE.md accuracy findings are covered in the Executive Summary and Phase 0.

---

## A process note on this review itself

One of the four background research agents used for this audit briefly ran `git stash --keep-index` on the live working tree while trying to get a clean baseline for a reflection check — against this repo's own standing rule to never stash (the rule is specifically about `-u`, since that lifts untracked `docs/` planning assets that may be mid-edit; this stash didn't include `-u` and so didn't touch untracked files). It caught its own mistake, ran `git stash pop` immediately, and reported it transparently rather than staying quiet. I independently re-verified afterward: `git stash list` is empty and every file the current uncommitted Area Map + movement work touches is still present and intact. No work was lost, but flagging it here in the interest of the same "don't just trust a self-report" standard this whole review was built on.
