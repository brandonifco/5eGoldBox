using System.Collections.Generic;
using System.IO;
using Godot;

// Loads and caches DungeonWallMaterial instances from the SBS asset
// packs under assets/. Textures are cached by material name so repeated
// Configure calls (every real move/turn) don't re-walk resource paths.
//
// File-name note: the pack itself is not internally consistent -- Layer
// 1's own Top/Right pieces use a hyphen before the piece name
// ("-Top-...", "-Right-...") while Bottom/Center/Left use an underscore
// ("_Bottom-...", "_Center-...", "_Left-..."), and the Door decoration's
// own Layer 2 files are misnamed "Layer_1" inside their filenames despite
// living in a "Layer 2" folder at the correct half-scale dimensions.
// Paths below were verified against the actual files, not derived from a
// template -- a second material (Brick 2/3, or a Cave Walls Addon
// material) would need its own paths verified the same way before
// assuming this same pattern holds.
internal static class DungeonWallMaterials
{
	private static readonly Dictionary<string, DungeonWallMaterial> Cache = new();

	// No real per-scenario/per-map material mapping exists yet -- every
	// current scenario (Watchtower, Hollow Mill, Sunken Chapel) is a
	// built stone/timber structure, none is a natural cave, so a single
	// default is an honest placeholder rather than invented content, the
	// same gap RealGameSession's own hardcoded DungeonCorridor SceneKey
	// already documents for the flat-color predecessor of this view.
	internal static DungeonWallMaterial ResolveDefault()
	{
		return LoadBrick1();
	}

	private static DungeonWallMaterial LoadBrick1()
	{
		const string Key = "Brick 1";

		if (Cache.TryGetValue(Key, out DungeonWallMaterial? cached))
		{
			return cached;
		}

		const string Base = "res://assets/dungeon-crawler-pack/Brick 1/";
		const string DoorBase = Base + "Brick 1 - Decorations/Door/";

		DungeonWallMaterial material = new()
		{
			Layer1Top = Load(Base + "Layer 1/Brick_01-Layer_1-Top-256x64.png"),
			Layer1Bottom = Load(Base + "Layer 1/Brick_01-Layer_1_Bottom-256x64.png"),
			Layer1Left = Load(Base + "Layer 1/Brick_01-Layer_1_Left-64x256.png"),
			Layer1Right = Load(Base + "Layer 1/Brick_01-Layer_1-Right-64x256.png"),
			Layer1Center = Load(Base + "Layer 1/Brick_01-Layer_1_Center-128x128.png"),
			Layer1DoorLeft = Load(DoorBase + "Layer 1/Brick_01-Layer_1_LDoor-64x256.png"),
			Layer1DoorCenter = Load(DoorBase + "Layer 1/Brick_01-Layer_1_CDoor-128x128.png"),
			Layer1DoorRight = Load(DoorBase + "Layer 1/Brick_01-Layer_1_RDoor-64x256.png"),

			Layer2Top = Load(Base + "Layer 2/Brick_01-Layer_2_Top-128x32.png"),
			Layer2Bottom = Load(Base + "Layer 2/Brick_01-Layer_2_Bottom-128x32.png"),
			Layer2Left = Load(Base + "Layer 2/Brick_01-Layer_2_Left-32x128.png"),
			Layer2Right = Load(Base + "Layer 2/Brick_01-Layer_2_Right-32x128.png"),
			Layer2Center = Load(Base + "Layer 2/Brick_01-Layer_2_Center-64x64.png"),
			Layer2DoorLeft = Load(DoorBase + "Layer 2/Brick_01-Layer_1_LDoor-32x128.png"),
			Layer2DoorCenter = Load(DoorBase + "Layer 2/Brick_01-Layer_1_CDoor-64x64.png"),
			Layer2DoorRight = Load(DoorBase + "Layer 2/Brick_01-Layer_1_RDoor-32x128.png"),
		};

		Cache[Key] = material;

		return material;
	}

	private static Texture2D Load(string path)
	{
		return GD.Load<Texture2D>(path)
			?? throw new FileNotFoundException(
				$"Missing dungeon wall texture at '{path}'.");
	}
}
