using System;
using System.Linq;
using Godot;

// Zoom/pan camera math — split out of CombatView.cs (see that file's
// header). Same discrete-preset/recenter-on-focus design as
// RegionalMapView.Zoom.cs (M7b): verifiable, and here the natural focus
// is whoever's turn it is rather than whatever's selected.
public partial class CombatView
{
	private static readonly float[] ZoomLevels = { 1.0f, 1.5f, 2.0f };

	public void CycleZoom(bool zoomIn)
	{
		int direction = zoomIn ? 1 : -1;
		_zoomIndex = Math.Clamp(_zoomIndex + direction, 0, ZoomLevels.Length - 1);
		ApplyZoomAndPan();
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

	// Pans/clamps against the full background rect (_combatImage.Size —
	// the placeholder or the mock art, whichever is showing), exactly
	// like the pre-isometric version did, NOT the diamond's own smaller,
	// letterboxed bounding box. The diamond is already centered within
	// that background by Metrics' own offsetX/offsetY; only the focus
	// point (below) needed to become isometric-aware. Clamping against
	// the diamond's bounds instead — tried first, reverted — shifted the
	// whole content group (background included) to chase the diamond's
	// center, cutting off the background's own far edge inside the
	// viewport even at the default zoom level.
	private void ApplyZoomAndPan()
	{
		float zoom = ZoomLevels[_zoomIndex];
		_combatContent.Scale = new Vector2(zoom, zoom);

		Vector2 viewportSize = _combatViewport.Size;
		Vector2 contentSize = _combatImage.Size * zoom;
		Vector2 focusPoint = ResolveFocusPoint() * zoom;

		Vector2 offset = (viewportSize / 2f) - focusPoint;
		Vector2 minOffset = new(
			Mathf.Min(0, viewportSize.X - contentSize.X),
			Mathf.Min(0, viewportSize.Y - contentSize.Y));

		_combatContent.Position = new Vector2(
			Mathf.Clamp(offset.X, minOffset.X, 0),
			Mathf.Clamp(offset.Y, minOffset.Y, 0));
	}

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
}
