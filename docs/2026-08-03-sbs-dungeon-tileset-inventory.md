# SBS dungeon/cave wall tileset — inventory

**Date:** 2026-08-03
**Location on disk:** `/home/brandon/Downloads/SBS - Dungeon Crawler Pack/` and
`/home/brandon/Downloads/SBS - Cave Walls Addon/` (not part of this repo, not committed — this doc
just records what's there). **License: CC0/Public Domain** (Screaming Brain Studios, stated
explicitly in each pack's own `License.txt`) — no usage restrictions, unlike the character-sprite
pack in `2026-08-03-medieval-heroes-asset-inventory.md`, which needed the user's own license
confirmation. Cataloged, not integrated — no code or asset files touched.

## Why this matters here

Same as the character-sprite pack: ties to Phase 3 of
`docs/2026-08-02-independent-review-and-redesign.md` (an art pass) — undecided which phase comes
next after Phase 1 closed. Recorded here so picking Phase 3 up later doesn't need re-deriving what
these packs contain. **This pack is a more direct fit than the character sprites for one specific
gap**: `AreaMapView`'s walkable/wall cells are flat colors today (confirmed in the earlier
Godot-exploration-map work this session), and this is a genuine wall-tile system.

## What's in each pack

**Dungeon Crawler Pack** (1.9MB) — 3 built-material wall sets (`Brick 1`/`Brick 2`/`Brick 3`), 34
files each:
- **`Layer 1`**: the base wall structure, five directional pieces — `Top`/`Bottom` (256×64),
  `Left`/`Right` (64×256), `Center` (128×128, `Brick 3`'s Center is 256×256 instead). Reads as a
  classic 9-slice/edge-tile wall-segment system: edge pieces tile along a wall's length, `Center`
  fills the middle.
- **`Layer 2`**: the same five directional pieces at exactly half size (`Top`/`Bottom` 128×32,
  `Left`/`Right` 32×128, `Center` 64×64) — an overlay/detail layer, not a separate material.
- **`{Brick N} - Decorations/`**: `Door`, `Window A`, `Window B`, `Window C` — each with its own
  `Center`/`Left`/`Right` pieces at the same size convention as the base wall (no `Top`/`Bottom`
  variant for these, since a door/window sits within a wall segment rather than spanning one), so
  they can substitute into a wall run built from the base pieces above.

**Cave Walls Addon** (1.6MB) — 9 natural-stone wall materials, same two-layer/five-piece structure,
no decorations: Copper Cave, Desert Cave, Limestone Cave, Marble Cave, Orange Stone Cave, Rough
Stone Cave, Sandstone Cave, Shale Cave, Sulphur Cave. Exactly the same dimensions as the Dungeon
Crawler Pack's `Layer 1`/`Layer 2` pieces (`Top`/`Bottom` 256×64, `Left`/`Right` 64×256, `Center`
128×128 / half-size equivalents) — the two packs are clearly meant to compose as one system, not
two independent ones.

**12 total wall materials** across both packs (3 built + 9 natural), all CC0.

## Where this could plug into the engine (not decided, not started)

- **`AreaMapView`** (`src/FiveEGoldBox.Godot/ui/presentation/AreaMapView*.cs`) draws walkable/wall/
  stair/door/treasure cells as flat colors via one `_Draw()` pass — the directional wall pieces
  here (edge + center, at consistent sizes) are close to a ready-made tileset for that view, more
  directly than the character sprites are for `CombatView`. Building this out would still need a
  real tileset/atlas pipeline in Godot, which doesn't exist yet (confirmed in the character-sprite
  inventory doc) — that gap applies here too, not solved by having the art in hand.
- **Content fit**: Hollow Mill (built stone/timber mill) and Watchtower (built stone tower) both
  read as "Brick"-style built structures; the Cave Walls materials would fit a natural cave/dungeon
  location if one gets authored — none of the three current scenarios is a natural cave.

## Open questions (not resolved here)

1. **Material-to-scenario mapping** — which of the 3 built materials or 9 cave materials (if any)
   represents which real location, is a real content decision, not inferred here.
2. **Tileset pipeline** — same gap flagged in the character-sprite inventory: no Godot
   tileset/atlas/9-slice rendering pipeline exists today. This pack's consistent piece-size
   convention (edges + center, two detail layers) is a reasonable shape to build one against, but
   that's still new engineering work, not something this cataloging pass starts.
3. **Relationship to the character-sprite pack** — both are now cataloged separately
   (`2026-08-03-medieval-heroes-asset-inventory.md` for characters, this doc for walls); whether a
   real Phase 3 pass tackles combatant art, wall/tile art, or both together is an open sequencing
   question, not decided here.
