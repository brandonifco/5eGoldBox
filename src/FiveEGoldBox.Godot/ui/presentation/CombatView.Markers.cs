using System;
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
	// diamond's width and height scale by the same (gridWidth+gridHeight)
	// factor, so locking the tile ratio locks the whole bounding box's
	// aspect ratio too, independent of grid shape — a 5x4 real encounter
	// and a 20x16 mock one render as the same "shape of tactics game,"
	// just scaled. Fitting both viewport dimensions independently would
	// instead make tile shape a function of (viewport aspect, W, H),
	// so different encounters would render with arbitrarily different
	// skews.
	private const float TileAspectRatio = 2f;

	private IsoMetrics Metrics
	{
		get
		{
			Vector2 imageSize = _combatImage.Size;
			int span = Math.Max(_gridWidth + _gridHeight, 1);

			float tileWidthFromWidth = 2f * imageSize.X / span;
			float tileWidthFromHeight = TileAspectRatio * 2f * imageSize.Y / span;
			float tileWidth = Mathf.Min(tileWidthFromWidth, tileWidthFromHeight);
			float tileHeight = tileWidth / TileAspectRatio;

			float diamondWidth = span * tileWidth / 2f;
			float diamondHeight = span * tileHeight / 2f;

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
				combatant.MaximumHitPoints);

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

		foreach (CombatHighlightViewModel highlight in _highlights)
		{
			CombatHighlightCell cell = new();
			_highlightsLayer.AddChild(cell);
			cell.Configure(highlight.Kind);

			int gridX = highlight.GridX;
			int gridY = highlight.GridY;
			cell.Pressed += () => CellActivated?.Invoke(gridX, gridY);

			PositionHighlight(cell, gridX, gridY);
			_highlightCells.Add(cell);
		}
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

	// The tactical grid itself, now a diamond lattice — each line of
	// constant gridY or gridX is still a straight screen-space line
	// since Project is affine per axis, just diagonal instead of
	// axis-aligned.
	private void DrawGridLines()
	{
		Color lineColor = new(1f, 1f, 1f, 0.18f);

		for (int gy = 0; gy <= _gridHeight; gy++)
		{
			_gridOverlay.DrawLine(Project(0, gy), Project(_gridWidth, gy), lineColor);
		}

		for (int gx = 0; gx <= _gridWidth; gx++)
		{
			_gridOverlay.DrawLine(Project(gx, 0), Project(gx, _gridHeight), lineColor);
		}
	}
}
