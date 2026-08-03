using Godot;

// A wall material's two textures for real 3D corridor rendering
// (DungeonCorridor3DView): a plain wall face and a plain door face.
// Unlike the earlier 2D-compositing approach, a real 3D camera handles
// perspective/distance itself, so there's no need for the pack's
// specially pre-skewed Top/Bottom/Left/Right edge pieces or its
// half-scale "far" Layer 2 set -- one flat, undistorted texture per
// kind is correct at any distance or wall orientation.
internal sealed class DungeonWallMaterial
{
	internal required Texture2D WallTexture { get; init; }
	internal required Texture2D DoorTexture { get; init; }
}
