internal sealed record CombatantMarkerViewModel(
	string Id,
	string Label,
	int GridX,
	int GridY,
	bool Active = false,
	bool Selected = false,
	bool IsAlly = true);
