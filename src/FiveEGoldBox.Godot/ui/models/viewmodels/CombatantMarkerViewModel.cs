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
	int? MaximumHitPoints = null,
	// A res:// path into the medieval-heroes asset pack, resolved from
	// the combatant's own stable ID by CombatantPortraitCatalog -- null
	// for mock content and for any real combatant the catalog doesn't
	// name (every monster today), which keeps the plain colored pin.
	string? PortraitResourcePath = null);
