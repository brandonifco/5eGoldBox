using System;
using System.Collections.Generic;
using System.Linq;

// Maps a real encounter's enemy roster (their shared MonsterIds, e.g.
// "monster.mill-rat" -- never a monster's own per-placement CombatantId,
// e.g. "combatant.mill-rat.first", which this would never match) to a
// piece of the SBS Isometric Floor Tiles pack
// (src/FiveEGoldBox.Godot/assets/isometric-floor-tiles/ -- see
// docs/2026-08-03-isometric-floor-tiles-asset-inventory.md), so CombatView
// can draw real tiles under the tactical grid instead of the placeholder's
// flat fill. No scenario/location ID reaches this deep into the real-
// combat integration layer today (RealCombatSession only ever sees
// ApplicationSessionState/CombatOperations), so the enemy roster itself is
// the signal -- each authored monster only ever appears in one scenario,
// the same "one battlefield per encounter" assumption
// CombatFloorTileCatalog's caller already relies on.
//
// Style choice is a real content decision, not derived from the pack
// (its 14 categories don't name a scenario) -- picked once here, by
// scenario setting: Watchtower is a ruined stone tower (Stone), Sunken
// Chapel is a religious interior (Tile), Hollow Mill is a timber
// grain mill (Wood). Each category ships two tile-art variants (01/02);
// "01" is used throughout, no particular reason to prefer "02".
internal static class CombatFloorTileCatalog
{
	private const string BasePath = "res://assets/isometric-floor-tiles/large-256x128";

	// 3 columns x 6 rows of 256x128 frames per sheet (18 color/pattern
	// variants of the same material) -- see the pack's own .tsx files
	// under assets/isometric-floor-tiles/large-256x128/tsx/.
	internal const int TileSourceWidth = 256;
	internal const int TileSourceHeight = 128;
	internal const int TileColumns = 3;
	internal const int TileVariantCount = 18;

	private static readonly IReadOnlyList<(string Prefix, string SheetPath)> ScenarioFloors = new[]
	{
		("monster.watchtower-raider.", $"{BasePath}/Interior/Stone/Floor_Stone_01-256x128.png"),
		("monster.chapel-guardian.", $"{BasePath}/Interior/Tile/Floor_Tile_01-256x128.png"),
		("monster.mill-", $"{BasePath}/Interior/Wood/Floor_Wood_01-256x128.png"),
	};

	// Null for an encounter whose enemies don't match any authored
	// scenario (e.g. a future ad-hoc test roster) -- CombatView falls
	// back to the placeholder fill in that case, the same as it always
	// has for every real encounter until this catalog existed.
	internal static string? Resolve(IEnumerable<string> monsterIds)
	{
		List<string> ids = monsterIds.ToList();

		foreach ((string prefix, string sheetPath) in ScenarioFloors)
		{
			if (ids.Any(id => id.StartsWith(prefix, StringComparison.Ordinal)))
			{
				return sheetPath;
			}
		}

		return null;
	}
}
