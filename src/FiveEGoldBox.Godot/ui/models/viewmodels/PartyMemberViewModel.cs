internal sealed record PartyMemberViewModel(
	string Id,
	string DisplayName,
	string HealthText,
	// Presentational only — how full a health bar should render, not a
	// game-rule quantity. The UI never derives this from raw HP itself.
	double HealthFraction,
	string? ConditionText = null,
	string? PortraitKey = null,
	bool Selected = false);
