# PVGames Apex Predators asset pack — inventory

**Date:** 2026-08-03.
**Location:** `src/FiveEGoldBox.Godot/assets/apex-predators/` — three cropped 128×128 portrait
PNGs only (~38KB total). The raw source packs (`PVGames_ApexPredators/`,
`PVGames_ApexPredators_SideViewBattlers/`, ~167MB combined) stay in the user's Downloads folder,
**not committed** — a deliberate scope call (see "Why only three files" below), unlike the
medieval-heroes pack which was committed in full.

**License:** PVGames' standard terms (`Read Me II.docx` in the raw pack) — usable in any
commercial/non-commercial project, edits allowed; the only restriction is reselling/redistributing
the raw resource pack itself. Same terms as medieval-heroes.

## What's in the raw pack (not committed)

Three monster types — `Apex_Predator`, `Apex_Hunter`, `Apex_Stalker` — reptilian/kaiju-style
beasts, each with:

- **`{Name}/Sprite_1.png`–`Sprite_8.png`** (4096×4096 each) — full 8-directional RPGMaker-MV-style
  overworld animation sheets (walk/run/idle/attack/skill/block/evade/hit/critical/woozy/behavior/
  dead), per `Reference - Monster Sheets.docx`'s frame layout. ~90MB total across all three.
- **`{Name}/{Name}_Large.png`** (2048×2048) — single reference/splash art.
- **`{Name}_SideViewBattler_Large.png`** (12000×4800, in the separate `_SideViewBattlers` pack) —
  a 16-column × 6-row grid of 750×800 battle-pose frames, same "MVsv" (RPGMaker MV sideview
  battler) convention the medieval-heroes portraits already use. ~60MB total across all three.

## What was extracted and why only three files

`CombatantMarkerPin.DrawPortrait` (`src/FiveEGoldBox.Godot/ui/components/combat/
CombatantMarkerPin.cs`) only ever samples the top-left 128×128 pixels of whatever texture it's
given (`PortraitFrameSize = 128f`) — the same "single static frame, no animation pipeline" scope
the medieval-heroes party portraits already committed to. Given that, importing the full 167MB of
mostly-unused animation frames (matching the medieval-heroes precedent of committing whole packs)
was weighed against just extracting what's actually usable today — the user chose the latter.

Per monster: row 0, col 0 of the `SideViewBattler_Large.png` grid (a clean walking-profile pose,
consistent across all three sheets), trimmed to its alpha bounding box, centered on a square
transparent canvas with ~12% padding, downscaled to exactly 128×128 so the whole file is the
sampled frame (matching how the medieval-heroes 384×128 strips have their first 128×128 cell be
the whole usable image). Source pixels only — no filters/recoloring applied.

## Role mapping (decided 2026-08-03)

None of the three creature names match an authored monster. The current roster
(`data/rulesets/campaign/core.json`) is `monster.watchtower-raider.marauder`/`archer`,
`monster.chapel-guardian.acolyte`/`sexton`, `monster.mill-rat`, `monster.mill-thrall` — five of six
are human/humanoid, where reptilian kaiju art would look wrong. Only `mill-rat` reads as a
plausible beast stand-in.

- **`ApexStalker` → `monster.mill-rat`**, wired in `CombatantPortraitCatalog.RolePortraits`
  (smallest/most feral-reading of the three).
- **`ApexPredator`/`ApexHunter`** — portraits extracted and committed, but **not wired to
  anything**, same "cataloged but inert" state as the cave-walls-addon pack. No current monster fits
  them; wiring either one is a future content decision (a new beast encounter, or replacing one of
  the other five if the setting ever wants a monstrous enemy there) — not guessed here.

## If a future animation pass happens

The raw packs (not in this repo) hold everything needed for a real attack/hit/dead animation
pipeline per monster, same as medieval-heroes' `_alt_*` strips — they're still on the user's local
machine if that phase is ever picked up. This doc's frame-layout notes above (grid dimensions, cell
size, `Reference - Monster Sheets.docx`'s frame-range table) are enough to re-derive the crop
without re-deriving the grid geometry from scratch.
