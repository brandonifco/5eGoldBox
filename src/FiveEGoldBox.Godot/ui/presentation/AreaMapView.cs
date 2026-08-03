using System.Collections.Generic;
using System.Linq;
using Godot;

// A real top-down grid map for exploration — a straight axis-aligned
// grid (not CombatView's isometric one; a different visual register from
// tactical combat, closer to a classic dungeon-crawl automap), showing
// the current floor's real walkable/blocked cells, stairs, and the
// party's real position/facing. This Control itself is purely a
// renderer — no click-to-move, no pan/zoom, the whole floor is auto-fit
// to the viewport in one shot; real movement while it's shown is driven
// externally (ShellInteractionController.RealSession.cs's
// EnterAreaMapMode reuses the same arrow-key DirectMovement pipeline the
// front-facing view uses) and reaches this view only through repeated
// Configure calls, not through any input this Control handles itself.
public partial class AreaMapView : Control
{
	private Control _gridLayer = null!;
	private Control _partyMarkerLayer = null!;
	private PartyDirectionMarker? _partyMarker;

	private int _width = 1;
	private int _height = 1;
	private HashSet<(int X, int Y)> _walkable = new();
	private HashSet<(int X, int Y)> _stairs = new();
	private HashSet<(int X, int Y)> _treasure = new();
	private Dictionary<((int X, int Y) A, (int X, int Y) B), (bool IsLocked, bool IsOpen, bool IsRevealed)> _doorEdges = new();
	private int _partyX;
	private int _partyY;

	public override void _Ready()
	{
		_gridLayer = GetNode<Control>("%GridLayer");
		_partyMarkerLayer = GetNode<Control>("%PartyMarkerLayer");

		_gridLayer.Draw += DrawGrid;
		Resized += RefreshLayout;
	}

	internal void Configure(AreaMapViewModel model)
	{
		_width = model.Width;
		_height = model.Height;
		_walkable = model.WalkableCells
			.Select(cell => (cell.X, cell.Y))
			.ToHashSet();
		_stairs = model.StairCells
			.Select(cell => (cell.X, cell.Y))
			.ToHashSet();
		_treasure = model.TreasureCells
			.Select(cell => (cell.X, cell.Y))
			.ToHashSet();
		// Every door is kept, revealed or not -- an unrevealed secret
		// door still occupies its edge and must draw as an ordinary,
		// indistinguishable wall (see DrawGrid's door-edge pass), not as
		// an invisible barrier that looks like open floor but silently
		// blocks movement.
		_doorEdges = model.Doors
			.ToDictionary(
				door => NormalizeEdge((door.A.X, door.A.Y), (door.B.X, door.B.Y)),
				door => (door.IsLocked, door.IsOpen, door.IsRevealed));
		_partyX = model.PartyX;
		_partyY = model.PartyY;

		if (_partyMarker is null)
		{
			_partyMarker = new PartyDirectionMarker();
			_partyMarkerLayer.AddChild(_partyMarker);
		}

		_partyMarker.Configure(model.PartyFacing);

		RefreshLayout();
	}

	private void RefreshLayout()
	{
		PositionPartyMarker();
		_gridLayer.QueueRedraw();
	}

	// Fits the whole floor to the viewport in one shot, centered — no
	// independent X/Y fit, so a cell is always square regardless of the
	// floor's own aspect ratio.
	private (float CellSize, float OffsetX, float OffsetY) Metrics
	{
		get
		{
			Vector2 viewportSize = Size;
			float cellSize = Mathf.Min(
				viewportSize.X / Mathf.Max(_width, 1),
				viewportSize.Y / Mathf.Max(_height, 1));
			float offsetX = (viewportSize.X - (cellSize * _width)) / 2f;
			float offsetY = (viewportSize.Y - (cellSize * _height)) / 2f;

			return (cellSize, offsetX, offsetY);
		}
	}

	private void PositionPartyMarker()
	{
		if (_partyMarker is null)
		{
			return;
		}

		(float cellSize, float offsetX, float offsetY) = Metrics;
		float diameter = cellSize * 0.7f;
		Vector2 cellCenter = new(
			offsetX + ((_partyX + 0.5f) * cellSize),
			offsetY + ((_partyY + 0.5f) * cellSize));

		_partyMarker.Size = new Vector2(diameter, diameter);
		_partyMarker.Position = cellCenter - (_partyMarker.Size / 2f);
		_partyMarker.PivotOffset = _partyMarker.Size / 2f;
	}

	// Hand-drawn dungeon-cartography palette: warm parchment paper, a
	// light stone-gray fill under the cross-hatch for blocked cells, and
	// a single near-black ink used for every line -- perimeter walls,
	// door leaves, stair/chest linework -- so the whole map reads as one
	// consistent pen, the way the reference map does. Only the party
	// marker and locked-door accent break from pure ink.
	private static readonly Color ParchmentColor = new(0.96f, 0.94f, 0.87f, 1f);
	private static readonly Color StoneFillColor = new(0.8f, 0.78f, 0.73f, 1f);
	private static readonly Color StoneHatchColor = new(0.42f, 0.4f, 0.36f, 1f);
	private static readonly Color InkColor = new(0.08f, 0.07f, 0.06f, 1f);
	private static readonly Color GridLineColor = new(0.5f, 0.45f, 0.35f, 0.3f);
	private static readonly Color LockedAccentColor = new(0.55f, 0.15f, 0.12f, 1f);
	private static readonly Color ChestLockColor = new(0.72f, 0.58f, 0.16f, 1f);

	private bool IsWalkableAt(int x, int y)
		=> x >= 0 && x < _width && y >= 0 && y < _height && _walkable.Contains((x, y));

	private static ((int X, int Y) A, (int X, int Y) B) NormalizeEdge(
		(int X, int Y) a,
		(int X, int Y) b)
		=> (a.X < b.X || (a.X == b.X && a.Y < b.Y)) ? (a, b) : (b, a);

	private void DrawGrid()
	{
		(float cellSize, float offsetX, float offsetY) = Metrics;
		float wallWidth = Mathf.Max(2.5f, cellSize * 0.08f);

		// The whole control gets the parchment base first, not just the
		// floor's own bounding box, so letterboxing around a
		// non-square floor still reads as paper rather than the
		// engine's own background showing through.
		_gridLayer.DrawRect(new Rect2(Vector2.Zero, Size), ParchmentColor);

		// A faint graph-paper grid across the floor's bounding box,
		// drawn before every other pass so a blocked cell's stone fill
		// mostly covers it (matching the reference, where the grid is
		// only clearly legible inside rooms) while it stays visible
		// through untouched parchment.
		for (int gy = 0; gy <= _height; gy++)
		{
			_gridLayer.DrawLine(
				new Vector2(offsetX, offsetY + (gy * cellSize)),
				new Vector2(offsetX + (_width * cellSize), offsetY + (gy * cellSize)),
				GridLineColor);
		}

		for (int gx = 0; gx <= _width; gx++)
		{
			_gridLayer.DrawLine(
				new Vector2(offsetX + (gx * cellSize), offsetY),
				new Vector2(offsetX + (gx * cellSize), offsetY + (_height * cellSize)),
				GridLineColor);
		}

		// Per-cell fill/icon. Walkable, stair and treasure cells all
		// leave the parchment bare as their base -- only blocked (solid
		// rock) cells get their own fill -- and stairs/treasure draw as
		// plain ink linework on top, the same pen as everything else.
		for (int y = 0; y < _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				CellVisualKind kind = ResolveCellKind(
					_stairs.Contains((x, y)),
					_treasure.Contains((x, y)),
					_walkable.Contains((x, y)));
				Vector2 topLeft = new(
					offsetX + (x * cellSize),
					offsetY + (y * cellSize));
				Rect2 cellRect = new(topLeft, new Vector2(cellSize, cellSize));

				switch (kind)
				{
					case CellVisualKind.Blocked:
						_gridLayer.DrawRect(cellRect, StoneFillColor);
						DrawStoneHatch(cellRect, StoneHatchColor);
						break;
					case CellVisualKind.Stair:
						DrawStairChevrons(cellRect, InkColor);
						break;
					case CellVisualKind.Treasure:
						DrawChest(cellRect, InkColor, ChestLockColor);
						break;
				}
			}
		}

		// The perimeter ink line -- the map's defining feature. Traced
		// wherever a walkable cell borders a blocked one (or the edge of
		// the floor itself), rather than relying on fill-color contrast
		// the way the old flat-color cells did. A revealed door on this
		// same edge breaks the line into a gap-and-leaf symbol instead
		// of a solid stroke. An unrevealed secret door draws as a plain,
		// unbroken wall -- both its flanking tiles are ordinary walkable
		// floor, so without this it would render as open passage that
		// silently blocks movement instead of an ordinary-looking wall.
		for (int x = 0; x <= _width; x++)
		{
			for (int y = 0; y < _height; y++)
			{
				bool left = IsWalkableAt(x - 1, y);
				bool right = IsWalkableAt(x, y);
				Vector2 start = new(offsetX + (x * cellSize), offsetY + (y * cellSize));
				Vector2 end = new(offsetX + (x * cellSize), offsetY + ((y + 1) * cellSize));

				if (_doorEdges.TryGetValue(NormalizeEdge((x - 1, y), (x, y)), out var door))
				{
					DrawWallOrDoorSegment(
						start, end, InkColor, LockedAccentColor, wallWidth,
						door.IsRevealed ? (door.IsLocked, door.IsOpen) : null);
				}
				else if (left != right)
				{
					DrawWallOrDoorSegment(start, end, InkColor, LockedAccentColor, wallWidth, null);
				}
			}
		}

		for (int y = 0; y <= _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				bool top = IsWalkableAt(x, y - 1);
				bool bottom = IsWalkableAt(x, y);
				Vector2 start = new(offsetX + (x * cellSize), offsetY + (y * cellSize));
				Vector2 end = new(offsetX + ((x + 1) * cellSize), offsetY + (y * cellSize));

				if (_doorEdges.TryGetValue(NormalizeEdge((x, y - 1), (x, y)), out var door))
				{
					DrawWallOrDoorSegment(
						start, end, InkColor, LockedAccentColor, wallWidth,
						door.IsRevealed ? (door.IsLocked, door.IsOpen) : null);
				}
				else if (top != bottom)
				{
					DrawWallOrDoorSegment(start, end, InkColor, LockedAccentColor, wallWidth, null);
				}
			}
		}
	}
}
