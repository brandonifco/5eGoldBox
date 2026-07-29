using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// Split into partials by concern once this crossed the governing plan's
// 250-line review threshold (§6.3) — same convention as AppShell's and
// ShellInteractionController's own splits. This file keeps node wiring,
// (re)configure, and the SelectionList reaction; marker/route drawing is
// in RegionalMapView.Markers.cs, zoom/pan camera math is in
// RegionalMapView.Zoom.cs. Pure code motion — no behavior changed by the
// split itself.
public partial class RegionalMapView : Control
{
	private Control _mapViewport = null!;
	private Control _mapContent = null!;
	private TextureRect _mapImage = null!;
	private Control _routeOverlay = null!;
	private Control _markersLayer = null!;
	private SelectionList _selectionPanel = null!;
	private TooltipPanel _inspectionPanel = null!;

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
		_inspectionPanel = GetNode<TooltipPanel>("%InspectionPanel");

		_selectionPanel.SelectionChanged += OnSelectionChanged;
		_selectionPanel.SelectionActivated += OnSelectionActivated;
		_routeOverlay.Draw += DrawRoutePreview;
		_mapImage.Resized += RepositionMarkers;
		_mapViewport.GuiInput += OnMapViewportGuiInput;
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
		UpdateInspectionPanel();
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
		UpdateInspectionPanel();
		ApplyZoomAndPan();
		_routeOverlay.QueueRedraw();
	}

	// M7f: location and route inspection in one panel rather than two —
	// a route preview only ever exists between the party and whatever's
	// currently selected, so there's nothing a separate "route tooltip"
	// would say that isn't already "here's where you're looking, and
	// there's a route shown to it."
	private void UpdateInspectionPanel()
	{
		RegionalMapMarkerViewModel? selected =
			_locationMarkers.FirstOrDefault(marker => marker.Selected);

		if (selected is null)
		{
			_inspectionPanel.HideTooltip();
			return;
		}

		string body = selected.Description ?? string.Empty;

		if (_routePreview is { Count: >= 2 })
		{
			body += string.IsNullOrEmpty(body)
				? "A route is shown from the outpost."
				: " A route is shown from the outpost.";
		}

		_inspectionPanel.ShowTooltip(selected.Label, body);
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
}
