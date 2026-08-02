internal sealed record AreaMapPointViewModel(int X, int Y);

// Look-only, unlike CombatViewModel — no Active/Selected states, no
// per-cell click targets, just the current floor's real geometry plus
// where the party is and which way they're facing.
internal sealed record AreaMapViewModel(
	int Width,
	int Height,
	IReadOnlyList<AreaMapPointViewModel> WalkableCells,
	IReadOnlyList<AreaMapPointViewModel> StairCells,
	IReadOnlyList<AreaMapPointViewModel> ClosedDoorCells,
	IReadOnlyList<AreaMapPointViewModel> LockedDoorCells,
	IReadOnlyList<AreaMapPointViewModel> TreasureCells,
	int PartyX,
	int PartyY,
	string PartyFacing);
