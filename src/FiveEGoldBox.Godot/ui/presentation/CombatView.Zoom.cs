using System;
using System.Linq;
using Godot;

// Zoom/pan camera math — split out of CombatView.cs (see that file's
// header). Same discrete-preset zoom RegionalMapView.Zoom.cs (M7b) uses,
// but pan is a persistent offset now, not a value recomputed to center
// on focus every call — the user asked for scrolling that only kicks in
// near an edge (mouse hover, or the focus tile getting close), not a
// camera that snaps back to center on every turn advance/highlight
// change. _panOffset is CombatView.cs's own field; this file owns every
// read and write of it.
public partial class CombatView
{
	private static readonly float[] ZoomLevels = { 1.0f, 1.5f, 2.0f };

	// Mouse-hover edge scroll: how close (screen pixels, independent of
	// zoom -- it's about how close to the physical viewport edge the
	// cursor is) triggers it, and how fast it scrolls. Focus-tile edge
	// scroll uses a tile-count margin instead (below), since "2 tiles
	// from the edge" is what the user actually asked for there.
	private const float EdgeScrollMarginPx = 48f;
	private const float EdgeScrollSpeedPxPerSec = 600f;
	private const int FocusEdgeDeadzoneTiles = 2;

	public override void _Process(double delta)
	{
		if (!Visible)
		{
			return;
		}

		ApplyEdgeAutoScroll((float)delta);
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

	// Only ever called from Configure (a fresh encounter) and CycleZoom
	// (an explicit zoom change) -- both are real "the view should
	// reorient" moments, unlike an ordinary Refresh, which only clamps.
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
		Vector2 contentSize = _combatImage.Size * zoom;
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

	// Two independent triggers, summed into one direction before
	// applying speed*delta so hitting both at once doesn't scroll
	// faster than either alone: the mouse hovering near the physical
	// viewport edge, and "the highlighted tile" (whoever's turn it is,
	// or the selected target -- ResolveFocusPoint's own definition,
	// reused here since it's the only singular "the highlighted tile"
	// concept this view already has) getting within
	// FocusEdgeDeadzoneTiles of that same edge, in current on-screen
	// tile units so it stays correct across zoom levels.
	private void ApplyEdgeAutoScroll(float deltaSeconds)
	{
		Vector2 direction = Vector2.Zero;

		if (TryGetLocalMousePosition(out Vector2 mouseLocal))
		{
			direction += ComputeEdgeDirection(
				mouseLocal,
				_combatViewport.Size,
				EdgeScrollMarginPx);
		}

		float zoom = ZoomLevels[_zoomIndex];
		float focusDeadzonePx = FocusEdgeDeadzoneTiles * Metrics.TileHeight * zoom;
		Vector2 focusScreenPoint = (ResolveFocusPoint() * zoom) + _panOffset;

		direction += ComputeEdgeDirection(
			focusScreenPoint,
			_combatViewport.Size,
			focusDeadzonePx);

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
