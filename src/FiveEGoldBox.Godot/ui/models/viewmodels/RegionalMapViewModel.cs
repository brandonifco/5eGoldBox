using System.Collections.Generic;

internal sealed record RegionalMapViewModel(
	string MapKey,
	RegionalMapMarkerViewModel? PartyMarker,
	IReadOnlyList<RegionalMapMarkerViewModel> LocationMarkers,
	string? SelectedLocationId = null,
	IReadOnlyList<RegionalMapPointViewModel>? RoutePreview = null);
