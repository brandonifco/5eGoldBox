internal sealed record RegionalMapMarkerViewModel(
	string Id,
	string Label,
	RegionalMapPointViewModel Position,
	bool Selected = false,
	string? Description = null);
