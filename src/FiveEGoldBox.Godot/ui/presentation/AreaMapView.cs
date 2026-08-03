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
	private List<((int X, int Y) A, (int X, int Y) B, bool IsLocked, bool IsOpen)> _doors = new();
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
		// An unrevealed secret door draws nothing at all here -- its two
		// cells already render as ordinary floor/wall by whatever
		// TraversablePositions itself says, exactly as if the door did
		// not exist, so nothing gives away that there's something to
		// find.
		_doors = model.Doors
			.Where(door => door.IsRevealed)
			.Select(door => ((door.A.X, door.A.Y), (door.B.X, door.B.Y), door.IsLocked, door.IsOpen))
			.ToList();
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

	private void DrawGrid()
	{
		(float cellSize, float offsetX, float offsetY) = Metrics;
		Color walkableColor = new(0.55f, 0.5f, 0.42f, 1f);
		Color blockedColor = new(0.12f, 0.11f, 0.1f, 1f);
		Color blockedHatchColor = new(0.24f, 0.22f, 0.19f, 1f);
		Color stairColor = new(0.35f, 0.6f, 0.85f, 1f);
		Color stairArrowColor = new(0.95f, 0.97f, 1f, 1f);
		Color closedDoorLeafColor = new(0.5f, 0.32f, 0.18f, 1f);
		Color lockedDoorLeafColor = new(0.55f, 0.15f, 0.12f, 1f);
		Color openDoorFrameColor = new(0.5f, 0.32f, 0.18f, 0.85f);
		Color doorKnobColor = new(0.85f, 0.75f, 0.35f, 1f);
		Color padlockBodyColor = new(0.15f, 0.15f, 0.15f, 1f);
		Color padlockShackleColor = new(0.85f, 0.75f, 0.35f, 1f);
		Color chestBodyColor = new(0.45f, 0.28f, 0.12f, 1f);
		Color chestLidColor = new(0.58f, 0.38f, 0.18f, 1f);
		Color chestLockColor = new(0.85f, 0.7f, 0.2f, 1f);
		Color gridLineColor = new(0f, 0f, 0f, 0.25f);

		// Treasure sits on a walkable-floor base, since the chest
		// overlay (drawn below) carries the kind's own color -- a solid
		// fill for the whole cell would read as a wall, not a floor
		// item.
		for (int y = 0; y < _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				CellVisualKind kind = ResolveCellKind(
					_stairs.Contains((x, y)),
					_treasure.Contains((x, y)),
					_walkable.Contains((x, y)));
				Color baseFill = kind switch
				{
					CellVisualKind.Stair => stairColor,
					CellVisualKind.Blocked => blockedColor,
					_ => walkableColor,
				};
				Vector2 topLeft = new(
					offsetX + (x * cellSize),
					offsetY + (y * cellSize));
				Rect2 cellRect = new(topLeft, new Vector2(cellSize, cellSize));

				_gridLayer.DrawRect(cellRect, baseFill);

				switch (kind)
				{
					case CellVisualKind.Blocked:
						DrawWallHatch(cellRect, blockedHatchColor);
						break;
					case CellVisualKind.Stair:
						DrawStairChevrons(cellRect, stairArrowColor);
						break;
					case CellVisualKind.Treasure:
						DrawChest(cellRect, chestBodyColor, chestLidColor, chestLockColor);
						break;
				}
			}
		}

		for (int gy = 0; gy <= _height; gy++)
		{
			_gridLayer.DrawLine(
				new Vector2(offsetX, offsetY + (gy * cellSize)),
				new Vector2(offsetX + (_width * cellSize), offsetY + (gy * cellSize)),
				gridLineColor);
		}

		for (int gx = 0; gx <= _width; gx++)
		{
			_gridLayer.DrawLine(
				new Vector2(offsetX + (gx * cellSize), offsetY),
				new Vector2(offsetX + (gx * cellSize), offsetY + (_height * cellSize)),
				gridLineColor);
		}

		// Doors sit on the shared edge between two cells rather than
		// filling a cell of their own -- reuses DrawDoorLeaf/DrawPadlock
		// unchanged by handing them a synthetic cell-sized rect centered
		// on the boundary point instead of on an actual cell, so the
		// same leaf/knob proportions read as "in the wall between the
		// two rooms" rather than "filling one of them." An opened door
		// keeps a marker too -- a hollow frame instead of a solid leaf --
		// rather than disappearing into indistinguishable open floor.
		foreach (((int X, int Y) a, (int X, int Y) b, bool isLocked, bool isOpen) in _doors)
		{
			Vector2 borderMidpoint = new(
				offsetX + ((a.X + b.X + 1) * 0.5f * cellSize),
				offsetY + ((a.Y + b.Y + 1) * 0.5f * cellSize));
			Rect2 edgeRect = new(
				borderMidpoint - new Vector2(cellSize / 2f, cellSize / 2f),
				new Vector2(cellSize, cellSize));

			if (isOpen)
			{
				DrawOpenDoorway(edgeRect, openDoorFrameColor);
			}
			else if (isLocked)
			{
				DrawDoorLeaf(edgeRect, lockedDoorLeafColor, doorKnobColor);
				DrawPadlock(edgeRect, padlockBodyColor, padlockShackleColor);
			}
			else
			{
				DrawDoorLeaf(edgeRect, closedDoorLeafColor, doorKnobColor);
			}
		}
	}
}
