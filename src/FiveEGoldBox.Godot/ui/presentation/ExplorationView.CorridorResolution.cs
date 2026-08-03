using System;
using System.Collections.Generic;
using System.Linq;

// Pure geometry over the same flattened AreaMapViewModel AreaMapView
// already consumes for the top-down map (RealGameSession.DescribeAreaMap)
// -- no new Application-side type needed, since a first-person view and
// a top-down map are just two different readings of the same
// current-floor snapshot. Godot-API-free, the same "can be compiled and
// exercised completely outside the engine" property
// AreaMapView.CellColorResolution.cs already established for its own
// cell-kind precedence.
//
// Floods outward from the party's position over every real, connected,
// open cell within range (not just a single straight lane ahead) --
// walls are recorded only at the real boundary between an open cell and
// a blocked one, so a branch or open room renders as itself rather than
// as a straight corridor with unexplained gaps or floating fragments.
public partial class ExplorationView
{
	internal enum CorridorCellKind
	{
		Open,
		Wall,
		Door,
		// A revealed door the party has already opened -- still drawn
		// (a doorway shouldn't disappear), but unlike Door it does not
		// block the flood-fill from continuing past it.
		OpenDoor
	}

	internal enum CorridorEdgeSide
	{
		Forward,
		Backward,
		Left,
		Right
	}

	// A visitable floor tile, in camera-relative grid units: Forward
	// counts steps along the party's own forward vector (0 = the
	// party's own cell), Lateral counts steps along the party's own
	// left vector (negative = left, positive = right).
	internal readonly record struct CorridorFloorCell(int Forward, int Lateral);

	// A real wall/door boundary on one edge of one floor cell -- the
	// only place geometry needs to be drawn, since two adjacent open
	// cells need nothing between them.
	internal readonly record struct CorridorWallSegment(
		int Forward,
		int Lateral,
		CorridorEdgeSide Side,
		CorridorCellKind Kind);

	internal sealed record CorridorGeometry(
		IReadOnlyList<CorridorFloorCell> FloorCells,
		IReadOnlyList<CorridorWallSegment> WallSegments);

	internal static CorridorGeometry ResolveCorridor(AreaMapViewModel map, int radius)
	{
		HashSet<(int X, int Y)> open = map.WalkableCells
			.Select(cell => (cell.X, cell.Y))
			.ToHashSet();
		// Every door, in every state -- an unrevealed secret still needs
		// to be looked up (so it can resolve as a plain wall rather than
		// silently falling through to "open, nothing here"), and an
		// opened door still needs to be looked up (so it keeps drawing
		// as a doorway instead of vanishing).
		Dictionary<((int X, int Y) A, (int X, int Y) B), (bool IsOpen, bool IsRevealed)> doorEdges =
			map.Doors.ToDictionary(
				door => NormalizeEdge((door.A.X, door.A.Y), (door.B.X, door.B.Y)),
				door => (door.IsOpen, door.IsRevealed));
		(int Dx, int Dy) forward = ForwardVector(map.PartyFacing);
		(int Dx, int Dy) left = LeftVector(map.PartyFacing);
		(int Dx, int Dy) backward = (-forward.Dx, -forward.Dy);
		(int Dx, int Dy) right = (-left.Dx, -left.Dy);

		List<CorridorFloorCell> floorCells = new();
		List<CorridorWallSegment> wallSegments = new();
		HashSet<(int F, int L)> visited = new() { (0, 0) };
		Queue<(int F, int L)> queue = new();

		queue.Enqueue((0, 0));

		while (queue.Count > 0)
		{
			(int f, int l) = queue.Dequeue();

			floorCells.Add(new CorridorFloorCell(f, l));

			// Subtracting l*left (not adding) is required for consistency
			// with the "Left" direction below decrementing the lateral
			// coordinate while stepping in the +left real vector --
			// getting this sign wrong meant a cell reached by going left
			// got re-derived back to the WRONG real map tile the moment
			// it was dequeued and its own neighbors were checked,
			// silently corrupting anything past the first ring around
			// the party. Found by hand-tracing a real reported case
			// (standing on the stairs at (2,0) facing North, where the
			// real open tile to the left re-derived to (3,0), a real
			// wall) rather than guessed.
			(int X, int Y) here = (
				map.PartyX + (f * forward.Dx) - (l * left.Dx),
				map.PartyY + (f * forward.Dy) - (l * left.Dy));

			VisitNeighbor(f, l, f + 1, l, CorridorEdgeSide.Forward, here, forward, radius, open, doorEdges, visited, queue, wallSegments);
			VisitNeighbor(f, l, f - 1, l, CorridorEdgeSide.Backward, here, backward, radius, open, doorEdges, visited, queue, wallSegments);
			VisitNeighbor(f, l, f, l - 1, CorridorEdgeSide.Left, here, left, radius, open, doorEdges, visited, queue, wallSegments);
			VisitNeighbor(f, l, f, l + 1, CorridorEdgeSide.Right, here, right, radius, open, doorEdges, visited, queue, wallSegments);
		}

		return new CorridorGeometry(floorCells, wallSegments);
	}

	private static void VisitNeighbor(
		int f,
		int l,
		int neighborF,
		int neighborL,
		CorridorEdgeSide side,
		(int X, int Y) here,
		(int Dx, int Dy) direction,
		int radius,
		HashSet<(int X, int Y)> open,
		Dictionary<((int X, int Y) A, (int X, int Y) B), (bool IsOpen, bool IsRevealed)> doorEdges,
		HashSet<(int F, int L)> visited,
		Queue<(int F, int L)> queue,
		List<CorridorWallSegment> wallSegments)
	{
		(int X, int Y) neighborReal = (here.X + direction.Dx, here.Y + direction.Dy);
		CorridorCellKind kind = ResolveKind(here, neighborReal, open, doorEdges);

		if (kind != CorridorCellKind.Open)
		{
			wallSegments.Add(new CorridorWallSegment(f, l, side, kind));
		}

		// Wall and closed Door both stop the flood here -- there is
		// nothing to see past either. Open and OpenDoor both continue:
		// an opened door still draws (the segment above), but the party
		// can walk through it, so whatever is beyond needs its own
		// geometry too.
		if (kind is CorridorCellKind.Wall or CorridorCellKind.Door)
		{
			return;
		}

		if (Math.Abs(neighborF) + Math.Abs(neighborL) > radius)
		{
			return;
		}

		if (visited.Add((neighborF, neighborL)))
		{
			queue.Enqueue((neighborF, neighborL));
		}
	}

	// A door sits on the edge between two tiles now, not on a tile of
	// its own, so this checks the specific edge between "here" and the
	// candidate neighbor rather than the neighbor's own position.
	// Unrevealed resolves as a plain Wall -- rendering it any other way,
	// including as "just open," would either give away or misrepresent
	// a secret that has not been found yet.
	private static CorridorCellKind ResolveKind(
		(int X, int Y) here,
		(int X, int Y) neighbor,
		HashSet<(int X, int Y)> open,
		Dictionary<((int X, int Y) A, (int X, int Y) B), (bool IsOpen, bool IsRevealed)> doorEdges)
	{
		if (doorEdges.TryGetValue(NormalizeEdge(here, neighbor), out (bool IsOpen, bool IsRevealed) door))
		{
			if (!door.IsRevealed)
			{
				return CorridorCellKind.Wall;
			}

			return door.IsOpen
				? CorridorCellKind.OpenDoor
				: CorridorCellKind.Door;
		}

		return open.Contains(neighbor)
			? CorridorCellKind.Open
			: CorridorCellKind.Wall;
	}

	// A stable, order-independent key for the edge between two tiles.
	private static ((int X, int Y) A, (int X, int Y) B) NormalizeEdge(
		(int X, int Y) a,
		(int X, int Y) b)
	{
		return a.X < b.X || (a.X == b.X && a.Y < b.Y)
			? (a, b)
			: (b, a);
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
