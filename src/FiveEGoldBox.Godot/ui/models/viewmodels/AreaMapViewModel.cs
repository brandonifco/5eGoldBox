internal sealed record AreaMapPointViewModel(int X, int Y);

// A door on the edge between two adjacent cells, in no particular order --
// mirrors FiveEGoldBox.Application.Exploration.ExplorationDoorEdge, which
// this is flattened from. Includes every door in every state (open or
// closed, revealed or not) rather than pre-filtering, for the same
// reason the Application-side type does: a renderer needs the full
// picture to draw an unrevealed door as a plain wall and an opened door
// as a still-visible doorway rather than either just disappearing.
internal sealed record AreaMapDoorEdgeViewModel(
    AreaMapPointViewModel A,
    AreaMapPointViewModel B,
    bool IsLocked,
    bool IsOpen,
    bool IsRevealed);

// Look-only, unlike CombatViewModel — no Active/Selected states, no
// per-cell click targets, just the current floor's real geometry plus
// where the party is and which way they're facing.
internal sealed record AreaMapViewModel(
    int Width,
    int Height,
    IReadOnlyList<AreaMapPointViewModel> WalkableCells,
    IReadOnlyList<AreaMapPointViewModel> StairCells,
    IReadOnlyList<AreaMapDoorEdgeViewModel> Doors,
    IReadOnlyList<AreaMapPointViewModel> TreasureCells,
    int PartyX,
    int PartyY,
    string PartyFacing);
