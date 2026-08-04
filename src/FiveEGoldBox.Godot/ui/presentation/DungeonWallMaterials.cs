using System.Collections.Generic;
using System.IO;
using Godot;

// Loads and caches DungeonWallMaterial instances from the SBS asset
// packs under assets/. Only the flat Center/CDoor pieces are needed now
// (see DungeonWallMaterial's own comment) -- textures are cached by
// material name so repeated Configure calls don't re-walk resource
// paths.
//
// The pack's own CDoor texture draws the archway only in its bottom
// ~40% (the rest is plain wall header) -- correct on a texture meant
// to tile freely above a much shorter door in a taller wall panel, but
// this renderer stretches the whole texture across one full wall-height
// quad (DungeonCorridor3DView's WallHeight), the same as any plain wall
// segment, so the door itself only ever read as roughly half as tall as
// the corridor -- reported live by the user ("doors look about 4 feet
// tall"). `_full-height.png` is a derived sibling (cropped to the
// archway starting where its brightness sharply drops, ~y=48 of 128,
// then stretched back to 128x128) so the door fills the same quad
// height a wall does, same technique already used elsewhere this
// session for off-center combat portraits.
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
			WallTexture = Load(Base + "Layer 1/Brick_01-Layer_1_Center-128x128.png"),
			DoorTexture = Load(DoorBase + "Layer 1/Brick_01-Layer_1_CDoor-128x128_full-height.png"),
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
