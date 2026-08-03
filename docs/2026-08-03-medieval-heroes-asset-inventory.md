# Medieval Heroes I asset pack — inventory

**Date:** 2026-08-03, updated 2026-08-03 when the pack was committed (PR #230).
**Location:** `src/FiveEGoldBox.Godot/assets/medieval-heroes/` — originally cataloged from the
user's Downloads folder (per the user's stated license for the pack), since moved into the repo
verbatim. Committed, but still entirely inert: nothing in any code references these paths yet.

## Why this matters here

Ties directly to Phase 3 of `docs/2026-08-02-independent-review-and-redesign.md` ("an art pass,
connected to the existing Blender pipeline") — deferred, undecided which phase comes next after
Phase 1 closed. This doc exists so picking Phase 3 up later doesn't need re-deriving what's
available.

## What's in the pack

8 character folders, ~77MB total. 7 are humanoid characters with a full, matching pose set; one
(`Saurial`) is an outlier — no sitting/sleeping/kneel/dead/collapse, but has `flying` instead —
reads as a creature/monster rather than a party member, not confirmed.

| Folder | File count | Pose set |
|---|---|---|
| Baenor | 67 | full (idle/walking/running/sitting/sleeping/kneel/dead/collapse/ko + combat) |
| Huntress | 67 | full |
| Leyanne | 67 | full |
| LordEsther | 60 | full |
| MasterGaerron | 60 | full |
| Naia | 60 | full |
| PaulHammerArm | 67 | full |
| Saurial | 23 | reduced — `flying` instead of ground/rest poses |

Per character, the asset categories (confirmed by file dimensions, one character checked in
detail):

- **`{Name}_MVsv.png`** (1152×768) — the combat sprite master sheet. A grid of animation frames,
  128×128 each (9 cols × 6 rows).
- **`{Name}_MVsv_alt_*.png`** (384×128, 3 frames each) — **the "smaller sprites for combat" the
  user specifically wants** — individual animation-state strips: `attack1`/`attack2`,
  `critical1`–`critical6`, `victory1`–`victory8`, `magic`, `shooting`/`shootingstance`,
  `martialartpunch`/`martialartcritical`/`martialartstance`, `stance1`–`stance6`, `dead1`–`dead3`.
- **`{Name}_default.png` / `_idle1.png` / `_idle2.png`** (384×512, 3×4 grid of 128×128) —
  overworld/field walking sprite sheets (RPG-Maker-MV-style, scaled up from the classic 48×48
  convention to 128×128).
- **`{Name}_walking.png` / `_running.png`** (1024×512) — dedicated movement animation strips, plus
  `_diag` variants of most poses for diagonal facing.
- **`{Name}_Mastersheet.png`** (3072×3072) — a single combined reference sheet with everything.
- **`{Name}_Paperdoll.png` / `_Bust.png`** (512×512) — portrait-style art.
- **`{Name}_Picture.png`** (1024×800) — a larger illustration.
- **`{Name}_Face.png`** (144×144) — small icon-style face art.
- Rest/status poses: `sitting1`/`sitting2`, `sleeping`, `kneel`, `dead`, `collapse`, `ko` — each
  with a `_diag` variant too.

## Where this could plug into the engine (not decided, not started)

- **`CombatView`'s tactical grid** (`src/FiveEGoldBox.Godot/ui/presentation/CombatView*.cs`) is
  currently a plain placeholder background with per-combatant HP shown as a thin ring arc — no
  combatant sprite art at all. The `_MVsv_alt_*` animation-state strips (attack/critical/victory/
  magic/dead) map directly onto combat states this engine already models (attack resolution,
  victory/defeat, spellcasting) — the best-aligned fit in the whole pack for what the user asked
  for specifically.
- **`AreaMapView`/exploration** currently renders the party as a single rotated-triangle marker
  (`PartyDirectionMarker`), not individual sprites — the `_default`/`_idle`/`_walking` field sheets
  would be a different, larger integration if ever wanted (not requested).
- **Character/portrait screens** (Godot's real Character screen, PR #226–227) currently show no
  art at all, just name/class/HP text — `_Face`/`_Bust`/`_Paperdoll` would fit there.

## Open questions (not resolved here — future scoping work if Phase 3 is picked up)

1. **Role mapping.** None of the 8 names (Baenor, Huntress, Leyanne, LordEsther, MasterGaerron,
   Naia, PaulHammerArm, Saurial) match existing party roles (Fighter/Rogue/Cleric/Wizard) or
   authored monsters (mill-rat, mill-thrall, giant rat, etc.) by name. Assigning which sprite
   represents which real character/monster is a real content decision, not inferred here.
   `Saurial`'s reduced pose set suggests monster/creature rather than playable-party use, but that
   is a guess, not confirmed with the user.
2. **Extraction pipeline.** No tooling exists yet to slice the `_MVsv.png` master sheet or the
   individual `_alt_*` strips into Godot-usable textures/animations. `CLAUDE.md`'s Phase 3 entry
   mentions "the existing combat-sprite-animation convention memory" as prior art for this kind of
   pipeline — **that convention isn't present in this session's own memory record and wasn't found
   documented anywhere in this repo**; if it refers to something concrete, it needs to be pointed to
   directly rather than assumed, before any extraction tooling is built.
3. **Depth of integration.** Whether to build a real Godot tileset/sprite-atlas pipeline (a
   genuinely new subsystem — CLAUDE.md already notes none exists today, flat colors only) versus a
   narrower one-off wiring for a first character, is a scope decision for whenever Phase 3 is
   greenlit, not assumed here.
