using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// Grid lines, combatant markers, and highlight cells — split out of
// CombatView.cs (see that file's header).
//
// The grid renders as a true isometric diamond (2:1 dimetric ratio, the
// classic tactics-game look), not the straight top-down axis-aligned
// grid this started as. (0,0) is the diamond's top vertex — increasing
// gridX moves down-and-right, increasing gridY moves down-and-left —
// keeping the original grid's top-left corner as the same recognizable
// landmark it always was, read as "viewed from up and to the top-left."
public partial class CombatView
{
	private readonly record struct IsoMetrics(
		float TileWidth,
		float TileHeight,
		float OffsetX,
		float OffsetY,
		float DiamondWidth,
		float DiamondHeight);

	// Locked rather than independently fit to the viewport: both the
	// diamond's width and height scale by the same reference-span
	// factor, so locking the tile ratio locks the whole bounding box's
	// aspect ratio too, independent of grid shape — a 5x4 real encounter
	// and a 20x16 mock one render as the same "shape of tactics game,"
	// just scaled. Fitting both viewport dimensions independently would
	// instead make tile shape a function of (viewport aspect, W, H),
	// so different encounters would render with arbitrarily different
	// skews.
	private const float TileAspectRatio = 2f;

	// Tile size used to be derived from the actual grid's own span
	// (gridWidth+gridHeight), so every battlefield -- no matter how
	// large -- scaled down to exactly fill the viewport with nothing
	// left over. That made "the playing field is bigger than what's
	// visible, scroll to see the rest" structurally impossible: there
	// was never anything left outside the frame. ReferenceSpan fixes
	// tile size against a constant instead (9, Watchtower's original
	// 5+4 -- the most "typical" existing encounter, chosen so nothing
	// already-shipped changes size), so a battlefield larger than that
	// genuinely overflows the viewport and needs the edge auto-scroll
	// (CombatView.Zoom.cs) to see the rest of it -- requested live by
	// the user right after that auto-scroll shipped.
	private const int ReferenceSpan = 9;

	private IsoMetrics Metrics
	{
		get
		{
			Vector2 imageSize = _combatImage.Size;
			int realSpan = Math.Max(_gridWidth + _gridHeight, 1);

			float tileWidthFromWidth = 2f * imageSize.X / ReferenceSpan;
			float tileWidthFromHeight = TileAspectRatio * 2f * imageSize.Y / ReferenceSpan;
			float tileWidth = Mathf.Min(tileWidthFromWidth, tileWidthFromHeight);
			float tileHeight = tileWidth / TileAspectRatio;

			// The diamond's own bounding box still scales with the real
			// grid, not the reference span -- a battlefield bigger than
			// ReferenceSpan needs a genuinely bigger diamond (that's the
			// whole point), just built from the fixed tile size above
			// rather than a tile size that shrinks to compensate.
			float diamondWidth = realSpan * tileWidth / 2f;
			float diamondHeight = realSpan * tileHeight / 2f;

			// Centers the diamond's bounding box within imageSize. The
			// + gridHeight*tileWidth/2 term exists because Project's raw
			// output (before this offset) for gridX=0 ranges from
			// -gridHeight*tileWidth/2 upward, not from 0.
			float offsetX = (imageSize.X - diamondWidth) / 2f +
				(_gridHeight * tileWidth / 2f);
			float offsetY = (imageSize.Y - diamondHeight) / 2f;

			return new IsoMetrics(
				tileWidth, tileHeight, offsetX, offsetY, diamondWidth, diamondHeight);
		}
	}

	// Converts continuous grid-space coordinates (cell corners at
	// integers, cell centers at x.5/y.5) to screen-space pixels within
	// _combatContent.
	private Vector2 Project(float gridX, float gridY)
	{
		IsoMetrics metrics = Metrics;
		float halfW = metrics.TileWidth / 2f;
		float halfH = metrics.TileHeight / 2f;

		return new Vector2(
			((gridX - gridY) * halfW) + metrics.OffsetX,
			((gridX + gridY) * halfH) + metrics.OffsetY);
	}

	private void RebuildCombatants()
	{
		foreach (CombatantMarkerPin pin in _combatantPins)
		{
			_combatantsLayer.RemoveChild(pin);
			pin.QueueFree();
		}

		_combatantPins.Clear();

		int allyIndex = 0;
		float? allyAverageScreenX = AverageProjectedScreenX(isAlly: true);
		float? enemyAverageScreenX = AverageProjectedScreenX(isAlly: false);

		foreach (CombatantMarkerViewModel combatant in _combatants)
		{
			CombatantMarkerPin pin = new();
			_combatantsLayer.AddChild(pin);
			pin.Configure(
				combatant.Label,
				allyIndex,
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

			if (combatant.IsAlly)
			{
				allyIndex++;
			}
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
	// either side can end up on either edge of the diamond lattice
	// depending on the encounter's own battlefield layout.
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

	private void PositionCombatant(CombatantMarkerPin pin, int gridX, int gridY)
	{
		IsoMetrics metrics = Metrics;
		// tileHeight, not min(width,height): with a 2:1 tile, tileHeight
		// is always the smaller dimension and the real limit on how much
		// vertical room a token has before it pokes into the neighboring
		// row's cell. tileWidth has 2x the headroom, never the binding
		// constraint.
		float diameter = metrics.TileHeight * 0.8f;
		Vector2 cellCenter = Project(gridX + 0.5f, gridY + 0.5f);

		pin.Size = new Vector2(diameter, diameter);
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

	// Arrow-key navigation for the full-battlefield move cursor (see
	// EnterRealCombatMoveTargeting) needs to move along the diamond's
	// own two axes, not Godot's default spatial nearest-neighbor guess
	// -- on an isometric lattice "the nearest focusable control above"
	// and "the tile that reads as directly above" aren't the same
	// thing. Per the user's own request: Up/Down walk the axis that
	// reads up-left/down-right on screen (grid X), Left/Right walk the
	// one that reads up-right/down-left (grid Y) -- matching this
	// file's own header convention ("increasing gridX moves
	// down-and-right, increasing gridY moves down-and-left").
	//
	// Only wired for cursor-mode cells (a dense, full-grid set, so
	// every direction always has a real neighbor to point at except at
	// the battlefield's own edge) -- attack/spell targeting's sparse
	// legal-target sets don't get this, since a directly-adjacent cell
	// in every direction isn't guaranteed to exist there, and Godot's
	// own automatic spatial neighbor-finding already works fine for
	// that smaller, scattered case.
	private static void WireCursorFocusNeighbors(
		Dictionary<(int X, int Y), CombatHighlightCell> cellsByPosition)
	{
		foreach (((int x, int y), CombatHighlightCell cell) in cellsByPosition)
		{
			cell.FocusNeighborTop = ResolveNeighborPath(cell, cellsByPosition, x - 1, y);
			cell.FocusNeighborBottom = ResolveNeighborPath(cell, cellsByPosition, x + 1, y);
			cell.FocusNeighborRight = ResolveNeighborPath(cell, cellsByPosition, x, y - 1);
			cell.FocusNeighborLeft = ResolveNeighborPath(cell, cellsByPosition, x, y + 1);
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

	// Project(gridX, gridY) (integer args) gives a cell's top vertex —
	// CombatHighlightCell's own diamond is drawn/hit-tested relative to
	// its node's top-left corner, so the node itself is positioned
	// tileWidth/2 to the left of that vertex.
	private void PositionHighlight(CombatHighlightCell cell, int gridX, int gridY)
	{
		IsoMetrics metrics = Metrics;
		Vector2 topVertex = Project(gridX, gridY);

		cell.Size = new Vector2(metrics.TileWidth, metrics.TileHeight);
		cell.Position = new Vector2(topVertex.X - (metrics.TileWidth / 2f), topVertex.Y);
	}

	// Extra rings of floor tiles drawn beyond the battlefield's own
	// bounds, so panning/zooming never reveals a hard edge where the
	// floor stops and the placeholder fill shows through. A fixed
	// margin, not derived from viewport/zoom bounds -- generous enough
	// to cover every current scenario's small battlefields at every
	// available zoom level, not a rigorous guarantee at arbitrary sizes.
	private const int FloorMarginTiles = 6;

	// Real isometric floor art (CombatFloorTileCatalog), drawn under the
	// grid lines/highlights/combatants layers. Each tile's source PNG is
	// already a diamond silhouette on a transparent 256x128 (or 128x64)
	// canvas — the same shape Project/IsoMetrics already computes per
	// cell — so drawing it as a plain axis-aligned rect region (no
	// rotation/polygon UV math) lands correctly, the same technique
	// CombatantMarkerPin.DrawPortrait already uses for portraits. Always
	// the sheet's first (row 0, col 0) variant now -- uniform, not the
	// deterministic-per-cell variety this started with, per the user's
	// own request.
	private void DrawFloorTiles()
	{
		if (_floorTileTexture is null)
		{
			return;
		}

		IsoMetrics metrics = Metrics;
		Rect2 sourceRect = new(
			Vector2.Zero,
			new Vector2(
				CombatFloorTileCatalog.TileSourceWidth,
				CombatFloorTileCatalog.TileSourceHeight));

		for (int gy = -FloorMarginTiles; gy < _gridHeight + FloorMarginTiles; gy++)
		{
			for (int gx = -FloorMarginTiles; gx < _gridWidth + FloorMarginTiles; gx++)
			{
				Vector2 topVertex = Project(gx, gy);
				Rect2 destinationRect = new(
					new Vector2(topVertex.X - (metrics.TileWidth / 2f), topVertex.Y),
					new Vector2(metrics.TileWidth, metrics.TileHeight));

				_floorLayer.DrawTextureRectRegion(_floorTileTexture, destinationRect, sourceRect);
			}
		}
	}

	// Used to draw the full lattice unconditionally; now only ever marks
	// one cell -- whichever CombatView.Zoom.cs's _hoveredCell currently
	// names (updated every frame from the mouse, see UpdateHoveredCell)
	// -- per the user's own request to stop showing grid lines
	// everywhere. Each side is still a straight screen-space line since
	// Project is affine per axis, just diagonal instead of axis-aligned.
	private void DrawGridLines()
	{
		if (_hoveredCell is not Vector2I hovered)
		{
			return;
		}

		Color lineColor = ResolveHoverColor(hovered);

		Vector2 top = Project(hovered.X, hovered.Y);
		Vector2 right = Project(hovered.X + 1, hovered.Y);
		Vector2 bottom = Project(hovered.X + 1, hovered.Y + 1);
		Vector2 left = Project(hovered.X, hovered.Y + 1);

		_gridOverlay.DrawLine(top, right, lineColor);
		_gridOverlay.DrawLine(right, bottom, lineColor);
		_gridOverlay.DrawLine(bottom, left, lineColor);
		_gridOverlay.DrawLine(left, top, lineColor);
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
