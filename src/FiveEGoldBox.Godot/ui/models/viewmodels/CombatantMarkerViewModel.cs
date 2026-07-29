internal sealed record CombatantMarkerViewModel(
	string Id,
	string Label,
	int GridX,
	int GridY,
	bool Active = false,
	bool Selected = false,
	bool IsAlly = true,
	// Null for mock content, which never had real HP to show. Real
	// combat's CombatOperations.Query reports this for every combatant
	// on every refresh, at zero extra cost — a board that never visibly
	// reflects damage would undercut the point of a real-combat demo.
	int? CurrentHitPoints = null,
	int? MaximumHitPoints = null);
