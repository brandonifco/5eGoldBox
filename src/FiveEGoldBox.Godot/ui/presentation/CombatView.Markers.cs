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

	// Real isometric floor art (CombatFloorTileCatalog), drawn under the
	// grid lines/highlights/combatants layers. Each tile's source PNG is
	// already a diamond silhouette on a transparent 256x128 (or 128x64)
	// canvas — the same shape Project/IsoMetrics already computes per
	// cell — so drawing it as a plain axis-aligned rect region (no
	// rotation/polygon UV math) lands correctly, the same technique
	// CombatantMarkerPin.DrawPortrait already uses for portraits.
	private void DrawFloorTiles()
	{
		if (_floorTileTexture is null)
		{
			return;
		}

		IsoMetrics metrics = Metrics;

		for (int gy = 0; gy < _gridHeight; gy++)
		{
			for (int gx = 0; gx < _gridWidth; gx++)
			{
				Vector2 topVertex = Project(gx, gy);
				Rect2 destinationRect = new(
					new Vector2(topVertex.X - (metrics.TileWidth / 2f), topVertex.Y),
					new Vector2(metrics.TileWidth, metrics.TileHeight));

				// Deterministic per-cell variant, not random, so a redraw
				// (zoom/resize/refresh) never makes the floor visibly
				// shuffle under the player.
				int variantIndex =
					((gx * 7) + (gy * 13)) % CombatFloorTileCatalog.TileVariantCount;
				int column = variantIndex % CombatFloorTileCatalog.TileColumns;
				int row = variantIndex / CombatFloorTileCatalog.TileColumns;
				Rect2 sourceRect = new(
					new Vector2(
						column * CombatFloorTileCatalog.TileSourceWidth,
						row * CombatFloorTileCatalog.TileSourceHeight),
					new Vector2(
						CombatFloorTileCatalog.TileSourceWidth,
						CombatFloorTileCatalog.TileSourceHeight));

				_floorLayer.DrawTextureRectRegion(_floorTileTexture, destinationRect, sourceRect);
			}
		}
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
