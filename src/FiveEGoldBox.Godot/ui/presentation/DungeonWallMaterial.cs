using Godot;

// One wall material's full piece set, both depth scales the SBS Dungeon
// Crawler / Cave Walls packs ship (see
// docs/2026-08-03-sbs-dungeon-tileset-inventory.md): Layer 1 (near, full
// size/brightness) and Layer 2 (far, half size/brightness), each a
// Top/Bottom/Left/Right/Center frame, plus a Door variant of
// Left/Center/Right swapped in wherever that depth's cell is a door
// rather than a plain wall (the pack's own Decorations folders have no
// Top/Bottom door piece, since a door sits within a wall segment rather
// than spanning one).
internal sealed class DungeonWallMaterial
{
	internal required Texture2D Layer1Top { get; init; }
	internal required Texture2D Layer1Bottom { get; init; }
	internal required Texture2D Layer1Left { get; init; }
	internal required Texture2D Layer1Right { get; init; }
	internal required Texture2D Layer1Center { get; init; }
	internal required Texture2D Layer1DoorLeft { get; init; }
	internal required Texture2D Layer1DoorCenter { get; init; }
	internal required Texture2D Layer1DoorRight { get; init; }

	internal required Texture2D Layer2Top { get; init; }
	internal required Texture2D Layer2Bottom { get; init; }
	internal required Texture2D Layer2Left { get; init; }
	internal required Texture2D Layer2Right { get; init; }
	internal required Texture2D Layer2Center { get; init; }
	internal required Texture2D Layer2DoorLeft { get; init; }
	internal required Texture2D Layer2DoorCenter { get; init; }
	internal required Texture2D Layer2DoorRight { get; init; }
}
