using System.Collections.Generic;

internal sealed record ExplorationViewModel(
	string SceneKey,
	string? FacingIndicatorText = null,
	string? CompassText = null,
	IReadOnlyList<string>? OverlayPrompts = null);
