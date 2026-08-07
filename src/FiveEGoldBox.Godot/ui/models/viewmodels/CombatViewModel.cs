using System.Collections.Generic;

internal sealed record CombatViewModel(
	int GridWidth,
	int GridHeight,
	IReadOnlyList<CombatantMarkerViewModel> Combatants,
	string? ActiveCombatantId = null,
	string? SelectedCombatantId = null,
	IReadOnlyList<CombatHighlightViewModel>? Highlights = null,
	IReadOnlyList<string>? InformationalOverlays = null,
	// A real encounter's battlefield has no calibrated art the way the
	// mock tactical scene does — CombatView shows a plain placeholder
	// instead when this is false, the same MapKey-driven toggle
	// RegionalMapView already established for its own real-vs-mock split.
	bool HasArtBackground = true,
	int RoundNumber = 1,
	// Null/empty for mock content, which never had a real turn order to
	// show — the initiative strip simply doesn't render for it, the same
	// optionality convention PortraitResourcePath already uses.
	IReadOnlyList<InitiativeEntryViewModel>? InitiativeOrder = null);

internal sealed record InitiativeEntryViewModel(
	string CombatantId,
	string Label,
	bool IsAlly,
	bool IsActive);
