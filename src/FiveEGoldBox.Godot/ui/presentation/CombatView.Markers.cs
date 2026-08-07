using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// Grid lines, combatant markers, and highlight cells — split out of
// CombatView.cs (see that file's header).
//
// The grid renders straight top-down on square tiles: (0,0) is the
// battlefield's top-left corner, increasing gridX moves right, and
// increasing gridY moves down.
//
// This replaced a 2:1 isometric diamond lattice, and the reason is worth
// keeping. The diamond was chosen for the modern tactics-game look
// (Divinity/XCOM/Fire Emblem), but it fought the Gold Box reference this
// project is actually built on -- Pool of Radiance's combat was an
// overhead square grid, not isometric -- and it made orthogonal arrow
// keys structurally unable to line up with the lattice, which needed an
// explicit FocusNeighbor axis remap to paper over. On a square grid the
// keys map 1:1 and that workaround disappears. The user's own verdict on
// the isometric version was that navigating it "sucks all around."
public partial class CombatView
{
	private readonly record struct GridMetrics(
		float TileSize,
		float OffsetX,
		float OffsetY,
		float GridPixelWidth,
		float GridPixelHeight);

	// Tile size is fixed against a constant rather than derived from the
	// actual grid's span, so a battlefield larger than the reference
	// genuinely overflows the viewport and needs the edge auto-scroll
	// (CombatView.Zoom.cs) to see the rest -- rather than every
	// battlefield shrinking to fit exactly, which made "the playing
	// field is bigger than what's visible" structurally impossible.
	// 9 keeps existing encounters (9x8, 8x8) filling roughly the frame
	// they already did.
	private const int ReferenceSpan = 9;

	// Flat floor, mirroring GameUiTheme.tres's EGA Revival palette. Kept
	// as literals rather than read from the theme because these are
	// canvas draws, not themed Controls -- if the theme's ground/surface
	// values change, change them here too.
	private static readonly Color FloorColor = new(0.10196078f, 0.12549020f, 0.20000000f);
	private static readonly Color FloorBlotchColor = new(0.08235294f, 0.10588235f, 0.17254902f);
	private static readonly Color FloorSpeckleColor = new(0.17254902f, 0.22352941f, 0.32156864f);
	private static readonly Color FloorOutsideColor = new(0.05490196f, 0.07058824f, 0.1254902f);
	private static readonly Color FloorGridColor = new(0.20784314f, 0.2509804f, 0.41568628f, 0.42f);

	private GridMetrics Metrics
	{
		get
		{
			Vector2 imageSize = _combatImage.Size;

			// Square tiles: one size, taken from whichever viewport
			// dimension is the binding constraint.
			float tileSize = Mathf.Min(
				imageSize.X / ReferenceSpan,
				imageSize.Y / ReferenceSpan);

			float gridPixelWidth = _gridWidth * tileSize;
			float gridPixelHeight = _gridHeight * tileSize;

			return new GridMetrics(
				tileSize,
				(imageSize.X - gridPixelWidth) / 2f,
				(imageSize.Y - gridPixelHeight) / 2f,
				gridPixelWidth,
				gridPixelHeight);
		}
	}

	// Converts continuous grid-space coordinates (cell corners at
	// integers, cell centers at x.5/y.5) to screen-space pixels within
	// _combatContent.
	private Vector2 Project(float gridX, float gridY)
	{
		GridMetrics metrics = Metrics;

		return new Vector2(
			(gridX * metrics.TileSize) + metrics.OffsetX,
			(gridY * metrics.TileSize) + metrics.OffsetY);
	}

	private void RebuildCombatants()
	{
		foreach (CombatantMarkerPin pin in _combatantPins)
		{
			_combatantsLayer.RemoveChild(pin);
			pin.QueueFree();
		}

		_combatantPins.Clear();

		float? allyAverageScreenX = AverageProjectedScreenX(isAlly: true);
		float? enemyAverageScreenX = AverageProjectedScreenX(isAlly: false);

		foreach (CombatantMarkerViewModel combatant in _combatants)
		{
			CombatantMarkerPin pin = new();
			_combatantsLayer.AddChild(pin);
			pin.Configure(
				combatant.Label,
				combatant.IsAlly,
				combatant.Active,
				combatant.Selected,
				combatant.CurrentHitPoints,
				combatant.MaximumHitPoints,
				ResolvePortrait(combatant.PortraitResourcePath),
				ShouldFlipPortrait(
					combatant,
					combatant.IsAlly ? enemyAverageScreenX : allyAverageScreenX));

			string combatantId = combatant.Id;
			pin.Pressed += () => CombatantActivated?.Invoke(combatantId);

			PositionCombatant(pin, combatant.GridX, combatant.GridY);
			_combatantPins.Add(pin);
		}
	}

	// No per-direction art exists (single static frame), so facing the
	// other way means mirroring the draw call (CombatantMarkerPin's own
	// _flipPortrait) rather than picking a different frame. Both art
	// sources default to the *same* facing -- confirmed by eye, not
	// assumed: medieval-heroes' own frames face left (checked when they
	// were re-cropped for centering, see CombatantPortraitCatalog's own
	// comment on that), matching the Apex Predators frames' facing
	// (docs/2026-08-03-apex-predators-asset-inventory.md). An earlier
	// version of this method assumed the RPGMaker-MV side-view-Actor
	// convention (party art faces right) for medieval-heroes specifically
	// -- wrong, this pack's own art doesn't follow it, caught live when
	// the party kept facing away from the enemy after that version
	// shipped. One rule for both sides now: flip when the opposing side's
	// average is to the right, never a fixed left/right rule, since
	// either side can end up on either edge of the battlefield depending
	// on the encounter's own layout.
	private bool ShouldFlipPortrait(
		CombatantMarkerViewModel combatant,
		float? opposingAverageScreenX)
	{
		if (opposingAverageScreenX is not float opposingScreenX)
		{
			return false;
		}

		float ownScreenX = Project(
			combatant.GridX + 0.5f,
			combatant.GridY + 0.5f).X;

		return opposingScreenX > ownScreenX;
	}

	// Null when that side has no living combatants left to average (the
	// last enemy just fell, say) -- ShouldFlipPortrait then leaves every
	// remaining combatant at its default facing rather than dividing by
	// zero or flipping toward a side that no longer exists.
	private float? AverageProjectedScreenX(bool isAlly)
	{
		List<float> screenXs = _combatants
			.Where(combatant => combatant.IsAlly == isAlly)
			.Select(combatant => Project(
				combatant.GridX + 0.5f,
				combatant.GridY + 0.5f).X)
			.ToList();

		return screenXs.Count == 0 ? null : screenXs.Average();
	}

	// GD.Load is resolved through Godot's own resource cache (keyed by
	// path), so repeated calls for the same combatant across redraws --
	// RebuildCombatants tears down and recreates every pin on each one --
	// cost a dictionary lookup, not a re-read from disk.
	private static Texture2D? ResolvePortrait(string? resourcePath)
	{
		return resourcePath is null
			? null
			: GD.Load<Texture2D>(resourcePath);
	}

	private void RepositionCombatants()
	{
		for (int index = 0; index < _combatants.Count; index++)
		{
			CombatantMarkerViewModel combatant = _combatants[index];
			PositionCombatant(_combatantPins[index], combatant.GridX, combatant.GridY);
		}
	}

	// Slightly inset from the full tile so a click near a cell's edge
	// still reaches the highlight cell underneath (targeting a square vs.
	// targeting whoever stands on it), and so adjacent tokens read as
	// separate rather than touching.
	private void PositionCombatant(CombatantMarkerPin pin, int gridX, int gridY)
	{
		GridMetrics metrics = Metrics;
		float extent = metrics.TileSize * 0.9f;
		Vector2 cellCenter = Project(gridX + 0.5f, gridY + 0.5f);

		pin.Size = new Vector2(extent, extent);
		pin.Position = cellCenter - (pin.Size / 2f);
	}

	private void RebuildHighlights()
	{
		foreach (CombatHighlightCell cell in _highlightCells)
		{
			_highlightsLayer.RemoveChild(cell);
			cell.QueueFree();
		}

		_highlightCells.Clear();
		// The cells this pointed at are about to be freed above; a real
		// focus change on whatever replaces them re-sets it via
		// FocusEntered below.
		_cursorFocusedCell = null;
		_cursorCellsByPosition.Clear();

		foreach (CombatHighlightViewModel highlight in _highlights)
		{
			CombatHighlightCell cell = new();
			_highlightsLayer.AddChild(cell);
			cell.Configure(highlight.Kind);

			int gridX = highlight.GridX;
			int gridY = highlight.GridY;
			cell.Pressed += () => CellActivated?.Invoke(gridX, gridY);
			cell.FocusEntered += () =>
			{
				_cursorFocusedCell = new Vector2I(gridX, gridY);
				CellCursorFocused?.Invoke(gridX, gridY);
			};

			PositionHighlight(cell, gridX, gridY);
			_highlightCells.Add(cell);

			if (highlight.Kind is "move-legal" or "move-illegal")
			{
				_cursorCellsByPosition[(gridX, gridY)] = cell;
			}
		}

		WireCursorFocusNeighbors(_cursorCellsByPosition);
	}

	// Focus for the whole combat view is a single pool, and combatant pins
	// sit on a layer above the highlight cells. Left focusable during
	// targeting, Godot's spatial neighbour search hops between combatants
	// -- party members included -- instead of between legal targets, which
	// is exactly what "the cursor just jumps from character to character,
	// not enemy" was. Move targeting hid this because its cells carry
	// explicit neighbours that bypass the search entirely.
	//
	// Clicking a combatant still works: a Button fires Pressed without
	// needing focus.
	// Attack and spell targeting resolve by combatant id through a pin's
	// own Pressed, so the keyboard cursor has to live on the pins. When a
	// target set is active only those pins are focusable and they are
	// wired into a cycle, so the arrow keys step between legal targets
	// instead of wandering across every combatant on the field -- which is
	// what "jumps from character to character, not enemy" was.
	//
	// Reapplied on every Refresh rather than set once, because a mid-
	// targeting refresh (a target selection, say) rebuilds every pin.
	private void ApplyCombatantFocusability()
	{
		if (_targetableCombatantIds.Count == 0)
		{
			foreach (CombatantMarkerPin pin in _combatantPins)
			{
				pin.FocusMode = Control.FocusModeEnum.All;
			}

			return;
		}

		List<CombatantMarkerPin> targetable = new();

		for (int index = 0; index < _combatants.Count; index++)
		{
			bool isTarget = _targetableCombatantIds.Contains(_combatants[index].Id);
			CombatantMarkerPin pin = _combatantPins[index];

			pin.FocusMode = isTarget
				? Control.FocusModeEnum.All
				: Control.FocusModeEnum.None;

			if (isTarget)
			{
				targetable.Add(pin);
			}
		}

		WireTargetCycle(targetable);

		if (targetable.Count > 0 && !targetable.Any(pin => pin.HasFocus()))
		{
			targetable[0].GrabFocus();
		}
	}

	// Declared by the interaction controller when combatant targeting
	// opens, cleared when it ends.
	internal void SetTargetableCombatants(IReadOnlyList<string>? combatantIds)
	{
		_targetableCombatantIds.Clear();

		if (combatantIds is not null)
		{
			foreach (string id in combatantIds)
			{
				_targetableCombatantIds.Add(id);
			}
		}

		ApplyCombatantFocusability();
	}

	// A ring in reading order: Right/Down advance, Left/Up go back, both
	// ends wrap. Wrapping rather than clamping because with two or three
	// scattered targets, stopping dead at the last one reads as stuck.
	private static void WireTargetCycle(List<CombatantMarkerPin> pins)
	{
		if (pins.Count == 0)
		{
			return;
		}

		for (int index = 0; index < pins.Count; index++)
		{
			CombatantMarkerPin pin = pins[index];
			NodePath next = pin.GetPathTo(pins[(index + 1) % pins.Count]);
			NodePath previous = pin.GetPathTo(
				pins[(index - 1 + pins.Count) % pins.Count]);

			pin.FocusNeighborRight = next;
			pin.FocusNeighborBottom = next;
			pin.FocusNeighborLeft = previous;
			pin.FocusNeighborTop = previous;
		}
	}


	// Gives the move cursor a real starting cell instead of leaving it
	// wherever Godot's default focus happens to land -- called right
	// after ShowCombatHighlights opens move targeting, with the active
	// combatant's own position, per the user's own request: the cursor
	// should start on whoever's turn it is, not somewhere arbitrary. A
	// silent no-op for any other highlight kind (attack/spell targeting),
	// since only move cells are tracked here -- the active combatant is
	// never a legal target for their own attack, so this was never a
	// meaningful starting point for those anyway.
	internal void FocusCell(int gridX, int gridY)
	{
		if (_cursorCellsByPosition.TryGetValue(
			(gridX, gridY),
			out CombatHighlightCell? cell))
		{
			cell.GrabFocus();
		}
	}

	// On the square grid the arrow keys and the lattice finally agree, so
	// these are the obvious mappings: Up is one row up, Left is one
	// column left. The isometric version had to remap the axes here
	// (Up walked grid X, Left walked grid Y) because "the tile that
	// reads as directly above" was not the tile directly above on a
	// diamond -- that remap is gone with the projection that forced it.
	//
	// Still wired explicitly rather than left to Godot's automatic
	// spatial nearest-neighbour search, for one reason the projection
	// change does not remove: self-referencing at the battlefield edge
	// (see ResolveNeighborPath) clamps the cursor there instead of
	// letting it jump across the board.
	//
	// Only wired for cursor-mode cells (a dense, full-grid set, so every
	// direction always has a real neighbour except at the edge) --
	// attack/spell targeting's sparse legal-target sets don't get this,
	// since a directly-adjacent cell in every direction isn't guaranteed
	// to exist there, and Godot's own automatic search already works
	// fine for that smaller, scattered case.
	private static void WireCursorFocusNeighbors(
		Dictionary<(int X, int Y), CombatHighlightCell> cellsByPosition)
	{
		foreach (((int x, int y), CombatHighlightCell cell) in cellsByPosition)
		{
			cell.FocusNeighborTop = ResolveNeighborPath(cell, cellsByPosition, x, y - 1);
			cell.FocusNeighborBottom = ResolveNeighborPath(cell, cellsByPosition, x, y + 1);
			cell.FocusNeighborLeft = ResolveNeighborPath(cell, cellsByPosition, x - 1, y);
			cell.FocusNeighborRight = ResolveNeighborPath(cell, cellsByPosition, x + 1, y);
		}
	}

	// Self-referencing when there's no real neighbor in that direction
	// (the battlefield's own edge) clamps the cursor there instead of
	// falling back to Godot's automatic guess, which would otherwise
	// jump somewhere off-axis and undo the whole point of wiring these
	// explicitly.
	private static NodePath ResolveNeighborPath(
		CombatHighlightCell cell,
		Dictionary<(int X, int Y), CombatHighlightCell> cellsByPosition,
		int x,
		int y)
	{
		CombatHighlightCell target = cellsByPosition.TryGetValue(
			(x, y),
			out CombatHighlightCell? neighbor)
				? neighbor
				: cell;

		return cell.GetPathTo(target);
	}

	private void RepositionHighlights()
	{
		for (int index = 0; index < _highlights.Count; index++)
		{
			CombatHighlightViewModel highlight = _highlights[index];
			PositionHighlight(_highlightCells[index], highlight.GridX, highlight.GridY);
		}
	}

	// Project(gridX, gridY) with integer args gives the cell's top-left
	// corner directly, which is also the node's own origin -- so unlike
	// the isometric version this needs no corrective offset.
	private void PositionHighlight(CombatHighlightCell cell, int gridX, int gridY)
	{
		GridMetrics metrics = Metrics;

		cell.Size = new Vector2(metrics.TileSize, metrics.TileSize);
		cell.Position = Project(gridX, gridY);
	}

	// Extra rings of floor drawn beyond the battlefield's own bounds, so
	// panning/zooming never reveals a hard edge where the floor stops and
	// the placeholder fill shows through. A fixed margin, not derived
	// from viewport/zoom bounds -- generous enough to cover every current
	// scenario's small battlefields at every available zoom level, not a
	// rigorous guarantee at arbitrary sizes.
	private const int FloorMarginTiles = 6;

	// One continuous ground, not a per-cell fill.
	//
	// This started as a two-tone checker, and the checker is what made the
	// battlefield read as a chess board rather than a place: it announces
	// the lattice on every square whether or not anything is happening
	// there. Everything decorative here is therefore placed by *position*
	// and deliberately does not line up with cell boundaries. A tiling
	// ground texture, if one is ever added, has to be sampled the same way
	// -- drawn per cell it would reintroduce exactly the seam pattern the
	// checker was creating.
	//
	// The margin beyond the battlefield stays a flat darker tone, so where
	// the playable area ends is legible rather than hidden -- which matters
	// more now that a battlefield can genuinely overflow the viewport.
	private void DrawFloorTiles()
	{
		// Mock combat ships a calibrated painted battlefield; drawing over
		// it would hide the very thing it exists to show. The original
		// version was gated by "is there a floor-tile texture", which only
		// real encounters ever set — this keeps that same split now that
		// the floor is drawn rather than sampled.
		if (_combatImage.Visible)
		{
			return;
		}

		GridMetrics metrics = Metrics;

		Rect2 outside = new(
			Project(-FloorMarginTiles, -FloorMarginTiles),
			new Vector2(
				(_gridWidth + (2 * FloorMarginTiles)) * metrics.TileSize,
				(_gridHeight + (2 * FloorMarginTiles)) * metrics.TileSize));
		_floorLayer.DrawRect(outside, FloorOutsideColor);

		Rect2 battlefield = new(
			Project(0, 0),
			new Vector2(metrics.GridPixelWidth, metrics.GridPixelHeight));
		_floorLayer.DrawRect(battlefield, FloorColor);

		DrawFloorVariation(battlefield);

		// The lattice is only drawn while something is being targeted --
		// see DrawBattlefieldGrid.
		if (_highlights.Count > 0)
		{
			DrawBattlefieldGrid(metrics);
		}
	}

	// Damp blotches and speckle, so a flat fill does not read as empty
	// paper. Positions come from a fixed-seed sequence rather than the
	// clock or the cell index: stable across the redraws RebuildCombatants
	// triggers constantly, and unrelated to the grid.
	private void DrawFloorVariation(Rect2 area)
	{
		uint seed = 0x9E3779B9;

		float Next()
		{
			seed = (seed * 1664525u) + 1013904223u;
			return ((seed >> 8) & 0xFFFF) / 65535f;
		}

		for (int i = 0; i < 7; i++)
		{
			Vector2 center = area.Position +
				new Vector2(Next() * area.Size.X, Next() * area.Size.Y);
			float radius = area.Size.X * (0.06f + (Next() * 0.10f));
			_floorLayer.DrawCircle(center, radius, FloorBlotchColor);
		}

		int speckleCount = Mathf.Clamp(
			(int)(area.Size.X * area.Size.Y / 1400f), 40, 420);

		for (int i = 0; i < speckleCount; i++)
		{
			Vector2 at = area.Position +
				new Vector2(Next() * area.Size.X, Next() * area.Size.Y);
			_floorLayer.DrawRect(new Rect2(at, new Vector2(1.5f, 1.5f)), FloorSpeckleColor);
		}
	}

	// Only drawn while a target or destination is being chosen, which is
	// when squares actually matter. The Gold Box originals drew no lattice
	// at all -- you inferred the squares from where the sprites stood --
	// and a permanently visible grid was the other half of what made this
	// look like a board game.
	private void DrawBattlefieldGrid(GridMetrics metrics)
	{
		Vector2 origin = Project(0, 0);
		float right = origin.X + metrics.GridPixelWidth;
		float bottom = origin.Y + metrics.GridPixelHeight;

		for (int gx = 0; gx <= _gridWidth; gx++)
		{
			float x = origin.X + (gx * metrics.TileSize);
			_floorLayer.DrawLine(
				new Vector2(x, origin.Y), new Vector2(x, bottom), FloorGridColor);
		}

		for (int gy = 0; gy <= _gridHeight; gy++)
		{
			float y = origin.Y + (gy * metrics.TileSize);
			_floorLayer.DrawLine(
				new Vector2(origin.X, y), new Vector2(right, y), FloorGridColor);
		}
	}

	// Marks whichever single cell CombatView.Zoom.cs's _hoveredCell names
	// (updated every frame from the mouse, see UpdateHoveredCell) -- per
	// the user's own request to stop showing grid lines everywhere. The
	// faint full lattice under the battlefield is DrawBattlefieldGrid's
	// job; this is the hover marker on top of it.
	private void DrawGridLines()
	{
		if (_hoveredCell is not Vector2I hovered)
		{
			return;
		}

		GridMetrics metrics = Metrics;

		_gridOverlay.DrawRect(
			new Rect2(
				Project(hovered.X, hovered.Y),
				new Vector2(metrics.TileSize, metrics.TileSize)),
			ResolveHoverColor(hovered),
			filled: false,
			width: 2f);
	}

	// During real-combat move targeting, the hovered cell is one of the
	// full-battlefield "move-legal"/"move-illegal" cursor cells (see
	// EnterRealCombatMoveTargeting) -- reads that same legality here so
	// hovering with the mouse shows the same yellow/red the keyboard
	// cursor shows, rather than a plain white outline that doesn't say
	// anything about whether the tile is actually reachable. Falls back
	// to the old neutral white outline for every other context (attack/
	// spell targeting, or no targeting at all).
	private Color ResolveHoverColor(Vector2I hovered)
	{
		CombatHighlightViewModel? highlight = _highlights.FirstOrDefault(
			h => h.GridX == hovered.X && h.GridY == hovered.Y);

		return highlight?.Kind switch
		{
			"move-legal" => CombatHighlightCell.CursorLegalColor,
			"move-illegal" => CombatHighlightCell.CursorIllegalColor,
			_ => new Color(1f, 1f, 1f, 0.6f),
		};
	}
}
