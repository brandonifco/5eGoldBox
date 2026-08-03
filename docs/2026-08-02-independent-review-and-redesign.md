# 5eGoldBox — Independent Review & Redesigned Plan

**Date:** 2026-08-02
**Method:** Four independent research passes (build/test verification, doc-vs-code accuracy audit, product/content assessment, process-overhead audit), each instructed to verify by running code and reading files rather than trusting CLAUDE.md or prior plan docs. Findings marked verified were actually run/read during this review, not re-quoted from an earlier document.

This is a checkpoint, not a permanent record. Update or delete sections as they close out, same as this project's other plan docs.

---

## Bottom line

The engine is in genuinely excellent shape. The game is not. Those are two different projects sharing one repo, and the last several weeks of work went almost entirely into the first one while the second one stood still. Of the last 60 commits, roughly 2 (~3%) added anything a player would notice; the rest was refactoring already-working code, moving already-existing content between formats, writing documentation about that motion, or building tools to edit content that doesn't exist yet in any real volume. Total playable content across all three scenarios is 15–30 minutes, and part of what looks like more than that — shop, inn, and temple screens in the Godot client — is disconnected mock dressing, not real content. That's the one finding in this review worth losing sleep over: **it's possible to open the game and see UI that implies far more game than exists**, which is worse than an honest placeholder.

None of this means stop what's been built — the foundation is worth having and was worth building carefully. It means the next phase of work should almost entirely stop being about the foundation.

---

## What's great (verified, not just claimed)

- **The engineering discipline is real.** `dotnet build` is 0 warnings on Debug and Release with `TreatWarningsAsErrors` on, verified by an actual run, not a doc claim. Zero `NoWarn`/`#pragma warning`/`SuppressMessage`/config severity overrides anywhere in the tree — also actually grepped, not assumed. Public API counts (176 exported types in Core, 64 in Application) were confirmed by loading the built DLLs and calling `GetExportedTypes()` directly — exact match to what CLAUDE.md claims.
- **The pluggable-adventure goal is genuinely achieved**, not just architecturally possible. Ruleset/scenario/campaign content all live in versioned JSON under `data/`, loaded through DTO'd loaders with schema validation and an environment-variable override for an external data root. Adding or editing an adventure today means editing JSON, not writing C# and recompiling. Three scenarios prove it by sharing zero vocabulary while running on identical code.
- **The Godot client is a real, backend-wired game**, not a shell. Outpost decisions, exploration, regional travel, tactical combat (move/attack/end turn), single-target spellcasting for all six spells, and a real top-down area map with doors/secret doors/treasure all play against the actual engine — confirmed by reading the integration code (`RealGameSession`, `RealCombatSession`), not by trusting the changelog.
- **Deterministic, characterization-tested combat** with frozen transcripts as a regression net, a real CI job that catches Godot build/boot regressions (2265+ tests, exactly one skip, run for real during this review), and a data-migration discipline (equivalence tests deleted only after each cutover) that held every time content moved from C# to JSON.
- **The `godot-ui` branch/worktree split resolved cleanly.** Verified `git merge-base --is-ancestor godot-ui main` is true — nothing was lost reconciling two lines of concurrent development back together.

None of this is aspirational. It's a legitimately strong technical foundation for a game. That's exactly why it's worth saying plainly that foundation work should now mostly stop.

---

## What's terrible

1. **The content-to-infrastructure ratio is badly inverted for a project whose stated goal is a finished game.** Three scenarios, 6 locations, exploration maps as small as 4 traversable cells, 4 encounters, 6 monsters, 6 spells, 11 weapons, 1 playable race. Realistic total playtime: 15–30 minutes. Meanwhile a full Blazor Server CRUD web app (~2,600 lines) was built to edit a total of 24 content records — JSON Schema autocomplete already covered the same editing surface for free. That's not a border case; for a solo author with no co-writers, this is the clearest instance in the project's history of engineering effort exceeding its actual payoff.
2. **The Godot client's shop/inn/temple screens are fake.** "Northhold's shop," "Mirefen's inn," "Seagate's temple" all render, but they live entirely in `ui/mocks/`, disconnected from the real engine and the three actual scenarios. Nothing in the real game reaches them. A player (or a returning developer three weeks from now) could easily mistake this for content that exists.
3. **The "no inventory/currency system" claim in both CLAUDE.md and code comments is wrong as stated.** A real `CurrencyAmount`/inventory/encumbrance system exists in Core with ten-plus dedicated test files — it's just never wired to treasure pickup or spending. The accurate claim is "no economy connects treasure to characters," which is a smaller, more fixable gap than "doesn't exist," and matters for planning: wiring an existing system to treasure is much cheaper than the doc implies.
4. **CLAUDE.md doesn't describe reality anymore, days after it was last true.** It says `docs/` "currently holds only the two tracked plan docs, the two rulebook references, and two durable planning assets" — actual `docs/` has 34 files and 32MB, including a 14MB tarball, ChatGPT-generated images, and closeout scripts. The test count (2165) is stale by 103 (actual 2268) because a whole test project (`ContentEditor.Tests`) landed the same day and the headline number was never bumped — the exact failure mode CLAUDE.md's own prior self-audit flagged and supposedly fixed.
5. **Process ceremony is uniform regardless of change size, and it's now visibly slowing things down.** 212 merge-PR commits plus 378 direct commits in about 24 days — roughly 9 PRs/day for a solo developer whose only reviewer is an AI in the same session. Three separate full branch→PR→CI→merge cycles landed in one afternoon solely to fix one-line stale status text in CLAUDE.md. An 8-line CI symlink fix got the same treatment. The ceremony is proportionate for real behavior changes; for a doc typo it's pure overhead, and it's part of *why* the doc typos keep needing their own PRs — every doc-sync fix competes for the same ceremony budget as real work.
6. **Six-plus planning documents overlap and periodically disagree**, and reconciling them is itself generating throwaway work: `priority-1-development-plan.md` (closed), the 2026-07-30 codebase reevaluation doc (largely closed), `Current Game Development Goals.txt` (the actual north star, still accurate, still untracked), the adventure-authoring-tool plan (all phases done), the data-driven-content plan (done), the content-editor plan (done), plus CLAUDE.md restating all of their outcomes inline. None of this is malicious drift — it's what happens when every closed-out plan is kept "just in case" instead of archived once its phases land.

---

## Where we should be, per the project's own long-term plan

`docs/Current Game Development Goals.txt` — the owner's own strategic document, still accurate, still the real north star — lays out 9+ phases. Re-reading it against where the code actually is:

| Phase | Goal | Status |
|---|---|---|
| 1 — Combat kernel stopping point | Deterministic, testable, UI-agnostic tactical combat | **Done**, and then some |
| 2 — Campaign/application backbone | A `CampaignSession`-shaped object coordinating party/time/mode/location | **Done** |
| 3 — Minimal text client | Prove the engine as a complete game without presentation logic | **Done** (Console) |
| 4 — Reproduce the loop in Godot | Crude but real Godot client proving the client boundary | **Done, and far exceeded** — Godot is now richer than the text client |
| 5 — Three complete game modes | Regional travel, exploration, tactical combat each minimally complete | **Done** |
| **6 — One real playable chapter** | Hub, several destinations, NPCs, shops, quests, dialogue, scripted events, multiple encounters, treasure that matters, meaningful choices, a real conclusion | **Not done — this is the actual gap.** Hollow Mill is the closest attempt (hub + dungeon + branching choice + two endings) but has no NPCs, no real shops, no dialogue, treasure that doesn't matter yet. The mock shop/inn/temple screens are what Phase 6 content would eventually need, built as decoration instead of as the real thing. |
| 7 — Expand mechanics only in service of content | Add spells/conditions/classes only when a content milestone needs them | **Held correctly** — this discipline hasn't slipped |
| 8 — Enemy AI as a product system | Legal-action discovery, target evaluation, tactical roles | Minimal (structural tactics only) — reasonable to defer, Phase 6 doesn't need more yet |
| 9 — Sustainable content-production pipeline | Tools *after* the engine and first chapter are stable | **Built ahead of need** — the tooling (Blazor content editor, Tiled importer, JSON Schema) is Phase 9-shaped work done before Phase 6 exists to justify it |

The plan document itself predicted this exact failure mode and named it explicitly: *"Once the engine and first chapter are stable, the project will shift increasingly from foundational engineering to content production."* The chapter isn't stable yet — it doesn't exist yet — and the shift to production tooling happened anyway.

---

## Redesigned plan from here

### Phase 0 — Cheap truth-and-hygiene pass (do first, no design decisions needed)

- Fix CLAUDE.md's stale claims: test count (2165 → 2268), the docs/ folder description, and re-date "Where things stand."
- Correct the "no inventory/currency system" claim to "not wired to treasure" in CLAUDE.md and the `TreasureDefinition.cs` comment.
- Archive the now-closed plan docs (`priority-1-development-plan.md`, the 2026-07-30 codebase-reevaluation doc, the adventure-authoring-tool plan, the data-driven-content plan, the content-editor plan, `5eGoldBox_Godot_UI_Remaining_Milestones_M4-M12.md`) to a new dated archive directory, the same pattern the 2026-07-25 sweep already established. Keep `Current Game Development Goals.txt` and the Scope Matrix as the only durable planning assets in `docs/`, plus this document while it's live.
- Move stray binaries (sprite PNGs/BMPs, ChatGPT images, screenshots, the 14MB tarball+checksum, closeout scripts) out of `docs/` — either delete if genuinely disposable or relocate to a non-`docs/` local-only folder.
- Delete the two disconnected mock screens (`ShowAreaMapScreen`'s already-flagged duplicate, and the shop/inn/temple mocks) or clearly label them "not connected to the real game" in-UI until Phase 1 below replaces them for real — whichever is cheaper. Leaving them silently indistinguishable from real content is the one item in this whole review that actively misleads rather than just under-delivers.

### Phase 0.5 — Process calibration (your call, not a default)

Proposed: reserve full branch→PR→CI→merge ceremony for actual behavior changes; commit doc-only fixes, comment fixes, and other zero-risk one-liners directly to `main`. This is a workflow preference, not an engineering fact — flagged for a decision below rather than just done.

### Phase 1 — Build the actual Phase 6 chapter (this is the real pivot)

Freeze new engineering/tooling work. Pick the Hollow Mill scenario (closest to real already: hub + dungeon + branching choice + two endings) and take it the rest of the way to what the goals doc actually asked for:

- Real NPCs and at least one real shop/service, wired to the real engine — either replacing the mock Northhold/Mirefen/Seagate screens with real equivalents at Hollow Mill, or deleting the mocks and building the real thing fresh, whichever is less work.
- Wire the existing (already-built, already-tested) currency/inventory system to treasure pickup, so treasure means something instead of flipping an inert flag.
- At least one real dialogue or quest-stage mechanism — doesn't need a scripting language, per the goals doc's own explicit warning against building one prematurely.
- Expand the exploration maps enough that a chapter feels like a chapter, not a hallway — the goals doc's own phrase, "not a collection of disconnected demonstrations," is presently a fair description of what exists.
- A real chapter conclusion the player can feel, not just a progress-marker flip.

### Phase 2 — Mechanics only where Phase 1 content demands them

Stay on the discipline that's already working (six spells, level 1, no leveling system, structural-only AI) and only add a mechanic when a specific piece of Phase 1 content needs it — not ahead of it. This phase is mostly "keep doing what's already being done," not new instruction.

### Phase 3 — Art pass, connected to the existing Blender pipeline

The area map and exploration views are still flat placeholder colors. This is the natural point to bring in the character/tile art already being produced outside this repo (per existing combat-sprite-animation convention memory) rather than adding more engine polish. Script/automate the pipeline rather than hand-placing assets, consistent with prior guidance on this kind of work.

### Phase 4 — Only after Phase 1 ships: return to production concerns

Re-open the deferred engineering backlog (dead-code decisions, doc-coverage pass, export/packaging pipeline — genuinely missing today, confirmed during this review: verification has only ever been a headless `godot --headless` run of the project directly, never a packaged export) once there's an actual chapter worth packaging.

---

## Decisions needed from you

1. **Greenlight the pivot** — freeze new tooling/refactor work and commit the next work cycle to Phase 1 (real chapter content) above, or push back on the framing if the ratio doesn't look as bad from where you sit.
2. **Process ceremony** — relax full PR ceremony for trivial fixes (Phase 0.5), or keep uniform ceremony deliberately (e.g., if the PR history itself is valued as a record).
3. **Mock screens** — delete the disconnected shop/inn/temple mocks now, or leave them until Phase 1 content replaces them, but either way stop treating them as equivalent to real content in status write-ups.
