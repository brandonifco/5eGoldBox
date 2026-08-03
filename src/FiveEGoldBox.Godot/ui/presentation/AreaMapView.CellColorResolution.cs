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
//
// Doors are no longer a per-cell kind here -- a door sits on the edge
// between two cells rather than occupying one of its own (see
// AreaMapDoorEdgeViewModel), so it's drawn as a separate overlay pass in
// AreaMapView.DrawGrid rather than folded into this precedence.
public partial class AreaMapView
{
	internal enum CellVisualKind
	{
		Blocked,
		Walkable,
		Treasure,
		Stair
	}

	// Precedence, highest to lowest: stair, treasure, walkable, blocked
	// (default).
	internal static CellVisualKind ResolveCellKind(
		bool isStair,
		bool isTreasure,
		bool isWalkable)
	{
		if (isStair)
		{
			return CellVisualKind.Stair;
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
