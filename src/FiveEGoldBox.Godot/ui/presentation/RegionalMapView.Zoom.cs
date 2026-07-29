using System;
using System.Linq;
using Godot;

// Zoom/pan camera math — split out of RegionalMapView.cs (see that file's
// header) once the combined class crossed the governing plan's 250-line
// review threshold.
public partial class RegionalMapView
{
	// M7b: discrete zoom presets, not continuous scale — same "cycle a
	// small named set" choice ShellLayoutController.CycleResolutionPreset
	// already made, for the same reason: verifiable without a display,
	// unlike a physics-like continuous camera.
	private static readonly float[] ZoomLevels = { 1.0f, 1.5f, 2.0f };

	public void CycleZoom(bool zoomIn)
	{
		int direction = zoomIn ? 1 : -1;
		_zoomIndex = Math.Clamp(_zoomIndex + direction, 0, ZoomLevels.Length - 1);
		ApplyZoomAndPan();
	}

	// M7b: mouse-wheel zoom handled locally, the same way a
	// ScrollContainer handles its own wheel input — keyboard zoom instead
	// goes through the central ShellInputRouter (see CycleZoom), this
	// project's established place for real player commands.
	private void OnMapViewportGuiInput(InputEvent inputEvent)
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

	// M7b "pan": rather than a manually dragged camera, the view
	// recenters on whichever location currently has the cursor — the
	// zoomed-in view always keeps what you're looking at in frame,
	// clamped so the map never scrolls past its own edges.
	private void ApplyZoomAndPan()
	{
		float zoom = ZoomLevels[_zoomIndex];
		_mapContent.Scale = new Vector2(zoom, zoom);

		Vector2 viewportSize = _mapViewport.Size;
		Vector2 mapSize = _mapImage.Size * zoom;
		Vector2 focusPoint = ResolveFocusPoint() * zoom;

		Vector2 offset = viewportSize / 2f - focusPoint;
		Vector2 minOffset = new(
			Mathf.Min(0, viewportSize.X - mapSize.X),
			Mathf.Min(0, viewportSize.Y - mapSize.Y));

		_mapContent.Position = new Vector2(
			Mathf.Clamp(offset.X, minOffset.X, 0),
			Mathf.Clamp(offset.Y, minOffset.Y, 0));
	}

	private Vector2 ResolveFocusPoint()
	{
		RegionalMapMarkerViewModel? selected =
			_locationMarkers.FirstOrDefault(marker => marker.Selected);
		RegionalMapPointViewModel position =
			selected?.Position ?? _partyMarker?.Position ?? new RegionalMapPointViewModel(0.5, 0.5);

		return new Vector2(
			(float)position.X * _mapImage.Size.X,
			(float)position.Y * _mapImage.Size.Y);
	}
}
