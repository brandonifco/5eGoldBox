using System.Collections.Generic;
using System.Linq;

// Pure geometry over the same flattened AreaMapViewModel AreaMapView
// already consumes for the top-down map (RealGameSession.DescribeAreaMap)
// -- no new Application-side type needed, since a first-person corridor
// and a top-down map are just two different readings of the same
// current-floor snapshot. Godot-API-free, the same "can be compiled and
// exercised completely outside the engine" property
// AreaMapView.CellColorResolution.cs already established for its own
// cell-kind precedence.
public partial class ExplorationView
{
	internal enum CorridorCellKind
	{
		Open,
		Wall,
		Door
	}

	// Depth counts forward steps from the party's own position (1 = the
	// boundary of the party's own standing cell; 2 = one cell further,
	// present only when depth 1's Ahead is Open). Left/Right describe the
	// sides of the cell the party would be standing in at (Depth - 1)
	// steps forward, so depth 1's Left/Right are the party's own current
	// side walls -- matching how classic first-person dungeon crawlers
	// (Wizardry, Bard's Tale, Dungeon Master) always render your own
	// cell's walls in the nearest layer, with each deeper layer nested
	// inside the previous one's forward-facing slot.
	internal readonly record struct CorridorDepthLayer(
		int Depth,
		CorridorCellKind Ahead,
		CorridorCellKind Left,
		CorridorCellKind Right);

	// Stops the walk the first depth whose Ahead is not Open -- there is
	// nothing to render past a wall or a closed/locked door, so no
	// further layers are produced beyond it.
	internal static IReadOnlyList<CorridorDepthLayer> ResolveCorridor(
		AreaMapViewModel map,
		int maxDepth)
	{
		HashSet<(int X, int Y)> open = map.WalkableCells
			.Select(cell => (cell.X, cell.Y))
			.ToHashSet();
		HashSet<(int X, int Y)> doors = map.ClosedDoorCells
			.Concat(map.LockedDoorCells)
			.Select(cell => (cell.X, cell.Y))
			.ToHashSet();
		(int Dx, int Dy) forward = ForwardVector(map.PartyFacing);
		(int Dx, int Dy) left = LeftVector(map.PartyFacing);

		List<CorridorDepthLayer> layers = new();
		(int X, int Y) standingCell = (map.PartyX, map.PartyY);

		for (int depth = 1; depth <= maxDepth; depth++)
		{
			(int X, int Y) aheadCell = (
				standingCell.X + forward.Dx,
				standingCell.Y + forward.Dy);
			(int X, int Y) leftCell = (
				standingCell.X + left.Dx,
				standingCell.Y + left.Dy);
			(int X, int Y) rightCell = (
				standingCell.X - left.Dx,
				standingCell.Y - left.Dy);
			CorridorCellKind aheadKind = ResolveKind(aheadCell, open, doors);

			layers.Add(new CorridorDepthLayer(
				depth,
				aheadKind,
				ResolveKind(leftCell, open, doors),
				ResolveKind(rightCell, open, doors)));

			if (aheadKind != CorridorCellKind.Open)
			{
				break;
			}

			standingCell = aheadCell;
		}

		return layers;
	}

	private static CorridorCellKind ResolveKind(
		(int X, int Y) position,
		HashSet<(int X, int Y)> open,
		HashSet<(int X, int Y)> doors)
	{
		if (doors.Contains(position))
		{
			return CorridorCellKind.Door;
		}

		return open.Contains(position)
			? CorridorCellKind.Open
			: CorridorCellKind.Wall;
	}

	private static (int Dx, int Dy) ForwardVector(string facing)
	{
		return facing switch
		{
			"North" => (0, -1),
			"East" => (1, 0),
			"South" => (0, 1),
			"West" => (-1, 0),
			_ => (0, -1)
		};
	}

	// "Camera left" -- North's own left is West, matching
	// ExplorationRules' TurnLeft convention on the Application side.
	private static (int Dx, int Dy) LeftVector(string facing)
	{
		return facing switch
		{
			"North" => (-1, 0),
			"East" => (0, -1),
			"South" => (1, 0),
			"West" => (0, 1),
			_ => (-1, 0)
		};
	}
}
