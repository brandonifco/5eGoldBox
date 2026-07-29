using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class RegionalMapView : Control
{
	// M7b: discrete zoom presets, not continuous scale — same "cycle a
	// small named set" choice ShellLayoutController.CycleResolutionPreset
	// already made, for the same reason: verifiable without a display,
	// unlike a physics-like continuous camera.
	private static readonly float[] ZoomLevels = { 1.0f, 1.5f, 2.0f };

	private Control _mapViewport = null!;
	private Control _mapContent = null!;
	private TextureRect _mapImage = null!;
	private Control _routeOverlay = null!;
	private Control _markersLayer = null!;
	private SelectionList _selectionPanel = null!;

	private readonly List<RegionalMapMarkerPin> _pins = new();
	private RegionalMapMarkerViewModel? _partyMarker;
	private IReadOnlyList<RegionalMapMarkerViewModel> _locationMarkers =
		Array.Empty<RegionalMapMarkerViewModel>();
	private IReadOnlyList<RegionalMapPointViewModel>? _routePreview;
	private int _zoomIndex;

	public event Action<string>? LocationFocused;
	public event Action<string>? LocationActivated;

	public override void _Ready()
	{
		_mapViewport = GetNode<Control>("%MapViewport");
		_mapContent = GetNode<Control>("%MapContent");
		_mapImage = GetNode<TextureRect>("%RegionalMapImage");
		_routeOverlay = GetNode<Control>("%RouteOverlay");
		_markersLayer = GetNode<Control>("%MarkersLayer");
		_selectionPanel = GetNode<SelectionList>("%SelectionPanel");

		_selectionPanel.SelectionChanged += OnSelectionChanged;
		_selectionPanel.SelectionActivated += OnSelectionActivated;
		_routeOverlay.Draw += DrawRoutePreview;
		_mapImage.Resized += RepositionMarkers;
	}

	// Full (re)configure — called whenever the regional map is (re)shown,
	// so zoom/pan reset to a clean default every time, the same way
	// ShellPresentationController.ShowExploration always resets facing.
	internal void Configure(RegionalMapViewModel model)
	{
		_partyMarker = model.PartyMarker;
		_locationMarkers = model.LocationMarkers;
		_routePreview = model.RoutePreview;
		_zoomIndex = 0;

		RebuildMarkers();
		PopulateSelectionList(model.SelectedLocationId);
		ApplyZoomAndPan();
		_routeOverlay.QueueRedraw();
	}

	// Lighter update for a selection change alone (SelectionList already
	// owns the change) — does not reset zoom, just what depends on which
	// location is current.
	internal void SetSelectedLocation(
		string? selectedLocationId,
		IReadOnlyList<RegionalMapPointViewModel>? routePreview)
	{
		_locationMarkers = _locationMarkers
			.Select(marker => marker with
			{
				Selected = marker.Id == selectedLocationId,
			})
			.ToList();
		_routePreview = routePreview;

		RebuildMarkers();
		ApplyZoomAndPan();
		_routeOverlay.QueueRedraw();
	}

	public void CycleZoom(bool zoomIn)
	{
		int direction = zoomIn ? 1 : -1;
		_zoomIndex = Math.Clamp(_zoomIndex + direction, 0, ZoomLevels.Length - 1);
		ApplyZoomAndPan();
	}

	private void OnSelectionChanged(int index, string itemId)
	{
		LocationFocused?.Invoke(itemId);
	}

	private void OnSelectionActivated(int index, string itemId)
	{
		LocationActivated?.Invoke(itemId);
	}

	private void PopulateSelectionList(string? selectedLocationId)
	{
		_selectionPanel.SetItems(_locationMarkers
			.Select(marker => new SelectionListEntry(marker.Id, marker.Label)));

		if (selectedLocationId is not null)
		{
			_selectionPanel.SelectId(selectedLocationId);
		}
	}

	private void RebuildMarkers()
	{
		foreach (RegionalMapMarkerPin pin in _pins)
		{
			_markersLayer.RemoveChild(pin);
			pin.QueueFree();
		}

		_pins.Clear();

		foreach (RegionalMapMarkerViewModel marker in _locationMarkers)
		{
			AddPin(marker.Position, RegionalMapMarkerPin.PinKind.Location, marker.Selected);
		}

		if (_partyMarker is not null)
		{
			AddPin(_partyMarker.Position, RegionalMapMarkerPin.PinKind.Party, selected: false);
		}
	}

	private void AddPin(
		RegionalMapPointViewModel position,
		RegionalMapMarkerPin.PinKind kind,
		bool selected)
	{
		RegionalMapMarkerPin pin = new();
		_markersLayer.AddChild(pin);
		pin.Configure(kind, selected);
		PositionPin(pin, position);
		_pins.Add(pin);
	}

	private void RepositionMarkers()
	{
		int index = 0;

		foreach (RegionalMapMarkerViewModel marker in _locationMarkers)
		{
			PositionPin(_pins[index], marker.Position);
			index++;
		}

		if (_partyMarker is not null)
		{
			PositionPin(_pins[index], _partyMarker.Position);
		}
	}

	private void PositionPin(RegionalMapMarkerPin pin, RegionalMapPointViewModel position)
	{
		Vector2 mapSize = _mapImage.Size;
		Vector2 center = new(
			(float)position.X * mapSize.X,
			(float)position.Y * mapSize.Y);
		float radius = pin.Diameter / 2f;

		pin.Position = center - new Vector2(radius, radius);
		pin.Size = new Vector2(pin.Diameter, pin.Diameter);
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

	// M7c: draws exactly the supplied points, nothing calculated —
	// "visual route previews without calculating authoritative paths."
	private void DrawRoutePreview()
	{
		if (_routePreview is not { Count: >= 2 })
		{
			return;
		}

		Vector2 mapSize = _mapImage.Size;
		Vector2[] points = _routePreview
			.Select(point => new Vector2(
				(float)point.X * mapSize.X,
				(float)point.Y * mapSize.Y))
			.ToArray();

		_routeOverlay.DrawPolyline(
			points,
			new Color(0.9254902f, 0.78039217f, 0.36078432f, 0.8f),
			3f,
			antialiased: true);
	}
}
