using System.Collections.Generic;

internal sealed record CombatViewModel(
	int GridWidth,
	int GridHeight,
	IReadOnlyList<CombatantMarkerViewModel> Combatants,
	string? ActiveCombatantId = null,
	string? SelectedCombatantId = null,
	IReadOnlyList<CombatHighlightViewModel>? Highlights = null,
	IReadOnlyList<string>? InformationalOverlays = null);
