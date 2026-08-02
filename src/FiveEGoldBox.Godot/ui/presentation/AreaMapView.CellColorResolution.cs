// Split out of AreaMapView.cs so the cell-kind precedence decision is a
// small, pure, Godot-API-free function — no Control/Color/Vector2 in this
// file at all, unlike CombatView.Markers.cs's own "pure projection math"
// split (which still leans on Godot's Vector2/Color types). That makes
// this the one piece of AreaMapView's rendering logic that can be
// compiled and exercised completely outside the engine, the same way
// RealGameSession and the view models it produces already are per
// CLAUDE.md's documented "throwaway console project" technique — there is
// no dedicated Godot C# test project in this repo to host a permanent
// test in.
public partial class AreaMapView
{
	internal enum CellVisualKind
	{
		Blocked,
		Walkable,
		Treasure,
		ClosedDoor,
		LockedDoor,
		Stair
	}

	// Precedence, highest to lowest: stair, locked door, closed door,
	// treasure, walkable, blocked (default). A door tile is never also a
	// stair/treasure tile (Phase B's own content validation rejects that
	// position collision), so in practice at most one of the first five
	// arguments is ever true at once -- this ordering exists to have a
	// defined answer regardless, not to resolve a real ambiguity.
	internal static CellVisualKind ResolveCellKind(
		bool isStair,
		bool isLockedDoor,
		bool isClosedDoor,
		bool isTreasure,
		bool isWalkable)
	{
		if (isStair)
		{
			return CellVisualKind.Stair;
		}

		if (isLockedDoor)
		{
			return CellVisualKind.LockedDoor;
		}

		if (isClosedDoor)
		{
			return CellVisualKind.ClosedDoor;
		}

		if (isTreasure)
		{
			return CellVisualKind.Treasure;
		}

		return isWalkable
			? CellVisualKind.Walkable
			: CellVisualKind.Blocked;
	}
}
