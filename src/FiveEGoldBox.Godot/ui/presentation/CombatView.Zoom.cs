using System;
using System.Linq;
using Godot;

// Zoom/pan camera math — split out of CombatView.cs (see that file's
// header). Same discrete-preset zoom RegionalMapView.Zoom.cs (M7b) uses,
// but pan is a persistent offset now, not a value recomputed to center
// on focus every call. Two separate mechanisms move it, deliberately not
// fighting each other: CombatView.cs's own Refresh recenters on the
// active combatant exactly when whose turn it is changes (a discrete
// snap, not a per-frame force); this file's ApplyEdgeAutoScroll is pure
// mouse-hover exploration in between turns, uncontested by anything else
// pulling toward a different point. An earlier version also pulled the
// pan toward keeping the active combatant within a tile margin of every
// edge, every frame -- summed with the mouse term, so scrolling toward
// more distant content and the focus term pulling back could cancel out
// within a couple of frames, reading as "scrolls a fraction of an inch
// and stops." Removed once turn-change recentering existed to cover the
// same need without a competing continuous force. _panOffset is
// CombatView.cs's own field; this file owns every read and write of it.
public partial class CombatView
{
	private static readonly float[] ZoomLevels = { 1.0f, 1.5f, 2.0f };

	// Mouse-hover edge scroll: how close (screen pixels, independent of
	// zoom -- it's about how close to the physical viewport edge the
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

	private void SetPanOffset(Vector2 desired)
	{
		float zoom = ZoomLevels[_zoomIndex];
		Vector2 viewportSize = _combatViewport.Size;
		// Whichever is actually bigger, the background image or the
		// diamond itself — Metrics.ReferenceSpan means a battlefield
		// larger than the reference now genuinely overflows the
		// background it's drawn on, and clamping against the background
		// alone (as this always did before that changed) would leave
		// the overflowing part impossible to pan to.
		IsoMetrics metrics = Metrics;
		Vector2 contentSize = new Vector2(
			Mathf.Max(_combatImage.Size.X, metrics.DiamondWidth),
			Mathf.Max(_combatImage.Size.Y, metrics.DiamondHeight)) * zoom;
		Vector2 minOffset = new(
			Mathf.Min(0, viewportSize.X - contentSize.X),
			Mathf.Min(0, viewportSize.Y - contentSize.Y));

		_panOffset = new Vector2(
			Mathf.Clamp(desired.X, minOffset.X, 0),
			Mathf.Clamp(desired.Y, minOffset.Y, 0));

		_combatContent.Position = _panOffset;
	}

	// Pans/clamps against the full background rect (_combatImage.Size —
	// the placeholder or the mock art, whichever is showing), exactly
	// like the pre-isometric version did, NOT the diamond's own smaller,
	// letterboxed bounding box. The diamond is already centered within
	// that background by Metrics' own offsetX/offsetY; only the focus
	// point (below) needed to become isometric-aware. Clamping against
	// the diamond's bounds instead — tried first, reverted — shifted the
	// whole content group (background included) to chase the diamond's
	// center, cutting off the background's own far edge inside the
	// viewport even at the default zoom level. SetPanOffset above is
	// what actually applies that clamp now; this comment describes why
	// it clamps against what it does.
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

	// Pure mouse-hover exploration: the only trigger is the cursor
	// sitting near the physical viewport edge. Whoever's turn it is gets
	// its own say over the camera through CombatView.cs's own
	// turn-change recenter instead, not a second continuous force here
	// that would fight this one (see this file's header).
	private void ApplyEdgeAutoScroll(float deltaSeconds)
	{
		if (!TryGetLocalMousePosition(out Vector2 mouseLocal))
		{
			return;
		}

		Vector2 direction = ComputeEdgeDirection(
			mouseLocal,
			_combatViewport.Size,
			EdgeScrollMarginPx);

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
