using System;
using System.Linq;
using Godot;

// Zoom/pan camera math — split out of CombatView.cs (see that file's
// header). Same discrete-preset zoom RegionalMapView.Zoom.cs (M7b) uses,
// but pan is a persistent offset now, not a value recomputed to center
// on focus every call. Two separate mechanisms move it, deliberately not
// fighting each other: CombatView.cs's own Refresh recenters on the
// active combatant exactly when whose turn it is changes (a discrete
// snap, not a per-frame force); this file's ApplyEdgeAutoScroll is a
// continuous keyboard-cursor-follow force in between turns, for the
// move cursor covering more of the battlefield than fits on screen.
// An earlier version also pulled the pan toward keeping the active
// combatant within a tile margin of every edge, every frame -- summed
// with a since-removed mouse-hover term, so scrolling toward more
// distant content and the focus term pulling back could cancel out
// within a couple of frames, reading as "scrolls a fraction of an inch
// and stops." Removed once turn-change recentering existed to cover the
// same need without a competing continuous force. The keyboard-cursor
// term (_cursorFocusedCell, CombatView.cs's own field) doesn't repeat
// that mistake: it pushes away from whichever edge the cursor is
// actually near, the same direction the player is already moving it,
// never pulls back toward a fixed anchor the way the old
// active-combatant force did. Mouse-hover scrolling was removed
// entirely per the user's own request -- TryGetLocalMousePosition
// stays, still used by the hover-highlight-outline feature
// (TryResolveHoveredCell) and by scroll-wheel zoom, neither of which
// this change touches. _panOffset is CombatView.cs's own field; this
// file owns every read and write of it.
public partial class CombatView
{
	private static readonly float[] ZoomLevels = { 1.0f, 1.5f, 2.0f };

	// Keyboard-cursor edge scroll: how close (screen pixels, independent
	// of zoom -- it's about how close to the physical viewport edge the
	// cursor is) triggers it, and how fast it scrolls.
	private const float EdgeScrollMarginPx = 48f;
	private const float EdgeScrollSpeedPxPerSec = 600f;

	public override void _Process(double delta)
	{
		if (!Visible)
		{
			return;
		}

		ApplyEdgeAutoScroll((float)delta);
		UpdateHoveredCell();
		UpdateDebugCameraLabel();
	}

	// The full-lattice grid used to always draw; the user asked for it
	// to only ever mark one cell at a time -- whichever the mouse is
	// over, or (not built yet, no keyboard tile cursor exists in this
	// view at all today) an arrow-key-driven selection. Redraws
	// GridOverlay only on an actual change, not every frame, the same
	// "don't touch the layer unless something about it changed"
	// discipline the floor/highlight layers already follow.
	private void UpdateHoveredCell()
	{
		Vector2I? hovered = TryResolveHoveredCell(out int gridX, out int gridY)
			? new Vector2I(gridX, gridY)
			: null;

		if (hovered == _hoveredCell)
		{
			return;
		}

		_hoveredCell = hovered;
		_gridOverlay.QueueRedraw();
	}

	// Inverts Project: screen-space mouse position -> the grid cell
	// containing it, or false if the mouse isn't over the viewport at
	// all or lands outside the real battlefield (the floor's own margin
	// tiles, or empty background). Continuous grid-space math, not
	// per-cell hit-testing -- correct regardless of current pan/zoom
	// since it undoes the exact same transform Project/_combatContent
	// apply.
	private bool TryResolveHoveredCell(out int gridX, out int gridY)
	{
		gridX = 0;
		gridY = 0;

		if (!TryGetLocalMousePosition(out Vector2 viewportLocal))
		{
			return false;
		}

		float zoom = ZoomLevels[_zoomIndex];
		Vector2 contentLocal = (viewportLocal - _panOffset) / zoom;

		IsoMetrics metrics = Metrics;
		float halfWidth = metrics.TileWidth / 2f;
		float halfHeight = metrics.TileHeight / 2f;

		float a = (contentLocal.X - metrics.OffsetX) / halfWidth;
		float b = (contentLocal.Y - metrics.OffsetY) / halfHeight;

		gridX = Mathf.FloorToInt((a + b) / 2f);
		gridY = Mathf.FloorToInt((b - a) / 2f);

		return gridX >= 0 && gridX < _gridWidth
			&& gridY >= 0 && gridY < _gridHeight;
	}

	public void CycleZoom(bool zoomIn)
	{
		int direction = zoomIn ? 1 : -1;
		_zoomIndex = Math.Clamp(_zoomIndex + direction, 0, ZoomLevels.Length - 1);
		_combatContent.Scale = new Vector2(ZoomLevels[_zoomIndex], ZoomLevels[_zoomIndex]);
		// A zoom change redraws the whole content group at a new scale
		// -- worth recentering on focus the same way a fresh Configure
		// does, rather than leaving the old offset (now against a
		// differently-sized content group) merely clamped valid.
		CenterPanOnFocus();
	}

	private void OnCombatViewportGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true } mouseEvent)
		{
			return;
		}

		if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
		{
			CycleZoom(zoomIn: true);
			AcceptEvent();
		}
		else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
		{
			CycleZoom(zoomIn: false);
			AcceptEvent();
		}
	}

	// Called from CombatView.cs's own Refresh whenever whose turn it is
	// changed (which a fresh Configure always counts as, see its own
	// comment) and from CycleZoom (an explicit zoom change) -- both are
	// real "the view should reorient" moments, unlike every other Refresh
	// call, which only clamps.
	private void CenterPanOnFocus()
	{
		float zoom = ZoomLevels[_zoomIndex];
		Vector2 focusPoint = ResolveFocusPoint() * zoom;

		SetPanOffset((_combatViewport.Size / 2f) - focusPoint);
	}

	// Re-validates the existing offset against the current zoom/content
	// size (e.g. after a window resize) without moving it otherwise --
	// see CombatView.cs's own Refresh/RepositionAll for why a recenter
	// would be the wrong call here.
	private void ClampPanOffset()
	{
		SetPanOffset(_panOffset);
	}

	// Found live, from the debug overlay's own numbers on an 8x8 real
	// battlefield: a focus point at content-local (278, -38) wanted a
	// desired pan of roughly (+79, +238) to center it (viewport half
	// minus focus), but the clamp always capped both axes at 0 -- so the
	// resulting pan was stuck at (0, 0) no matter what CenterPanOnFocus
	// asked for. Wrong assumption: the old clamp treated content as
	// always starting at content-local (0, 0) and only ever needing
	// negative pan to reveal more toward the bottom-right. That held
	// when a diamond always fit inside the background image; it stopped
	// holding once ReferenceSpan could center a diamond *bigger* than
	// the image, which pushes real tiles into negative content-local
	// coordinates on the diamond's own top-left -- content a
	// clamp-to-[min, 0] can never pan far enough right/down to reach.
	private void SetPanOffset(Vector2 desired)
	{
		float zoom = ZoomLevels[_zoomIndex];
		Vector2 viewportSize = _combatViewport.Size;
		IsoMetrics metrics = Metrics;
		float halfW = metrics.TileWidth / 2f;
		float halfH = metrics.TileHeight / 2f;

		// The diamond's real bounding box, from Project's own extremes --
		// not derived from DiamondWidth/DiamondHeight alone, which are
		// only symmetric around OffsetX for a square grid. Leftmost is
		// grid (0, gridHeight), rightmost is (gridWidth, 0), topmost is
		// (0, 0) (OffsetY itself, by construction), bottommost is
		// (gridWidth, gridHeight).
		float diamondMinX = (-_gridHeight * halfW) + metrics.OffsetX;
		float diamondMaxX = (_gridWidth * halfW) + metrics.OffsetX;
		float diamondMinY = metrics.OffsetY;
		float diamondMaxY = ((_gridWidth + _gridHeight) * halfH) + metrics.OffsetY;

		// Union with the background image's own rect ([0, 0]-imageSize) —
		// whichever extends further in each direction, background or
		// diamond, is what pan needs to keep fully covering the viewport.
		float unionMinX = Mathf.Min(0f, diamondMinX);
		float unionMaxX = Mathf.Max(_combatImage.Size.X, diamondMaxX);
		float unionMinY = Mathf.Min(0f, diamondMinY);
		float unionMaxY = Mathf.Max(_combatImage.Size.Y, diamondMaxY);

		_panOffset = new Vector2(
			Mathf.Clamp(
				desired.X,
				viewportSize.X - (unionMaxX * zoom),
				-unionMinX * zoom),
			Mathf.Clamp(
				desired.Y,
				viewportSize.Y - (unionMaxY * zoom),
				-unionMinY * zoom));

		_combatContent.Position = _panOffset;
	}

	// Purely a content-local point -- SetPanOffset above is what turns it
	// into an actual clamped pan offset, against the union of the
	// background rect and the diamond's own real bounding box (see its
	// own comment). This function itself needs no clamp awareness at
	// all; it just answers "where, in content-local space, is the thing
	// the camera should look at."
	private Vector2 ResolveFocusPoint()
	{
		CombatantMarkerViewModel? focus =
			_combatants.FirstOrDefault(combatant => combatant.Selected) ??
				_combatants.FirstOrDefault(combatant => combatant.Active);

		if (focus is null)
		{
			// Project(W/2, H/2) is exactly the diamond's own centroid.
			return Project(_gridWidth / 2f, _gridHeight / 2f);
		}

		return Project(focus.GridX + 0.5f, focus.GridY + 0.5f);
	}

	// Pushes away from whichever edge the keyboard cursor is near, the
	// same direction the player is already moving it, never pulling back
	// toward a fixed anchor. Whoever's turn it is gets its own say over
	// the camera through CombatView.cs's own turn-change recenter instead
	// of a continuous force here. Mouse-hover used to contribute a second
	// term here too, summed the same way; removed per the user's own
	// request, so only the keyboard cursor scrolls the view now.
	private void ApplyEdgeAutoScroll(float deltaSeconds)
	{
		Vector2 direction = Vector2.Zero;

		if (_cursorFocusedCell is Vector2I cursorCell)
		{
			float zoom = ZoomLevels[_zoomIndex];
			Vector2 cursorScreenPoint =
				(Project(cursorCell.X + 0.5f, cursorCell.Y + 0.5f) * zoom) + _panOffset;

			direction += ComputeEdgeDirection(
				cursorScreenPoint,
				_combatViewport.Size,
				EdgeScrollMarginPx);
		}

		if (direction == Vector2.Zero)
		{
			return;
		}

		Vector2 scroll = direction.LimitLength(1f) * EdgeScrollSpeedPxPerSec * deltaSeconds;
		SetPanOffset(_panOffset + scroll);
	}

	// False (and leaves localPosition unset) whenever the cursor isn't
	// actually over the viewport's own screen rect -- GetLocalMousePosition
	// alone is a pure coordinate transform and would happily report a
	// misleading in-range position for a cursor hovering some unrelated
	// part of the UI that transforms into the same local space.
	private bool TryGetLocalMousePosition(out Vector2 localPosition)
	{
		Rect2 globalRect = new(_combatViewport.GlobalPosition, _combatViewport.Size);

		if (!globalRect.HasPoint(_combatViewport.GetGlobalMousePosition()))
		{
			localPosition = Vector2.Zero;
			return false;
		}

		localPosition = _combatViewport.GetLocalMousePosition();
		return true;
	}

	// A unit-ish direction (each axis independently -1/0/1) pointing
	// which way to scroll so `point` moves away from whichever edge of
	// `bounds` it's within `marginPx` of. Deliberately direction only,
	// not distance -- ApplyEdgeAutoScroll normalizes the combined result
	// itself so a point already past an edge doesn't scroll faster than
	// one just inside the margin.
	private static Vector2 ComputeEdgeDirection(
		Vector2 point,
		Vector2 bounds,
		float marginPx)
	{
		float x = 0f;
		float y = 0f;

		if (point.X < marginPx)
		{
			x = 1f;
		}
		else if (point.X > bounds.X - marginPx)
		{
			x = -1f;
		}

		if (point.Y < marginPx)
		{
			y = 1f;
		}
		else if (point.Y > bounds.Y - marginPx)
		{
			y = -1f;
		}

		return new Vector2(x, y);
	}
}
