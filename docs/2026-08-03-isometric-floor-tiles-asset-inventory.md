# SBS Isometric Floor Tiles asset pack — inventory and wiring

**Date:** 2026-08-03.
**Location:** `src/FiveEGoldBox.Godot/assets/isometric-floor-tiles/` — committed in full (both
resolutions, all 14 materials, the Tiled `.tsx` metadata, and the license text), ~24MB, small
enough that "commit everything" (the medieval-heroes precedent) made more sense than the
crop-only call made for the much larger Apex Predators pack
(`docs/2026-08-03-apex-predators-asset-inventory.md`).

**License:** Screaming Brain Studios' standard CC0/Public Domain release — no restrictions, credit
optional. Same publisher and terms as the dungeon-crawler-pack/cave-walls-addon packs already in
the repo. `License.txt` is committed alongside the assets.

## What's in the pack

Two resolutions (`large-256x128/`, `small-128x64/`, same 2:1 tile aspect, just scaled), each with
14 materials — 7 interior (`Metal`, `Grill`, `Tile`, `Stone`, `Brick`, `Pattern`, `Wood`) and 7
exterior (`Stones`, `Elements`, `Grass`, `Rocky`, `Ice`, `Flora`, `Dry`) — two art variants per
material (`_01`/`_02`). Each `{Material}_{NN}-{size}.png` is a 768×768 (large) or 384×384 (small)
sheet: a 3-column × 6-row grid of individual diamond floor tiles (18 color/pattern variants of the
same material, not edge/transition tiles), confirmed against the pack's own `.tsx` Tiled tileset
metadata (kept under each resolution's `tsx/` folder).

**Source assets used chroma-key transparency (magenta, `#FF00FF`), not real alpha** — every PNG in
this repo was re-saved with that magenta converted to actual alpha before committing, so no
runtime chroma-key shader is needed; `GD.Load<Texture2D>` on any of these files just works.

## Wiring: real floor tiles in `CombatView` (2026-08-03)

`CombatView`'s isometric grid (`CombatView.Markers.cs`, 2:1 dimetric projection) previously drew
only a flat placeholder fill for real encounters — no floor art. Each tile's source PNG is already
a diamond silhouette on a transparent 256×128 canvas, the exact shape `Project`/`IsoMetrics`
already computes per cell, so `DrawFloorTiles` (new) draws each cell as a plain axis-aligned
`DrawTextureRectRegion` call — no rotation or polygon UV math needed — the same technique
`CombatantMarkerPin.DrawPortrait` already established for portraits. A deterministic
`(gx*7 + gy*13) % 18` picks one of the 18 color variants per cell, so the floor reads as natural
texture variety rather than an obviously-tiled repeat, without needing a persisted per-cell
assignment (redraws — zoom, resize, refresh — never visibly shuffle the floor).

**Material selection is scenario-keyed**, via a new `CombatFloorTileCatalog`
(`ui/integration/CombatFloorTileCatalog.cs`), the same shape as `CombatantPortraitCatalog`: no
scenario/location ID reaches this deep into the real-combat integration layer today
(`RealCombatSession` only ever sees `ApplicationSessionState`), so the encounter's own enemy
roster is the resolving signal instead — each authored monster only appears in one scenario.

| Scenario | Monster ID prefix | Material |
|---|---|---|
| Watchtower | `monster.watchtower-raider.` | Interior/Stone (ruined stone tower) |
| Sunken Chapel | `monster.chapel-guardian.` | Interior/Tile (religious interior) |
| Hollow Mill | `monster.mill-` | Interior/Wood (timber grain mill) |

Only the `_01` variant of each chosen material is wired; `_02` and every unused material/resolution
stay committed-but-inert, cataloged here for later if a scenario ever wants a different look.
`large-256x128` is the resolution actually loaded (`CombatFloorTileCatalog.BasePath`) — `Project`
scales the destination rect to whatever `IsoMetrics` computes at runtime regardless of source
resolution, so `small-128x64` is unused today, kept only for completeness.

`CombatViewModel` gained `FloorTileSheetPath` (nullable, defaults to `null` — mock content is
unaffected, still shows its own calibrated background). `RealCombatSession.Describe()` resolves it
from the live encounter's combatants each round-trip.

## Verification

Full solution Debug+Release build (0 warnings), full test suite (2289 passed, 3 skipped, unaffected
— Godot-only change), Godot build (0 warnings), headless boot (exit 0, empty stderr). **Not yet
played in the editor** — no screenshot/automation tooling (`xdotool`/`scrot`/`import`) is installed
in this environment, so the actual rendered look (tile scale, seam alignment, deterministic-variant
pattern) needs a real live check. To reach a real encounter with floor art wired: jump to Hollow
Mill's upper floor next to its mill-rat trigger and walk into it —

```
FIVEEGOLDBOX_DEBUG_LOCATION_ID=location.hollow-mill-house FIVEEGOLDBOX_DEBUG_FLOOR=UpperFloor FIVEEGOLDBOX_DEBUG_X=2 FIVEEGOLDBOX_DEBUG_Y=0 FIVEEGOLDBOX_DEBUG_FACING=East FIVEEGOLDBOX_DEBUG_PROGRESS_ID=mill.herbalist-consulted godot --path src/FiveEGoldBox.Godot
```

then move one step east into `(3, 0)` to trigger `encounter.mill-vermin-swarm` (two mill-rats —
also the first live check for the Apex Predators portrait wired the same day).
