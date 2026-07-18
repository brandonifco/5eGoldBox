namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterBattlefieldState
{
    public required string BattlefieldId { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required IReadOnlyList<GridPosition>
        BlockedPositions { get; init; }

    public IReadOnlyList<EncounterCoverPosition>
        CoverPositions { get; init; }
            = Array.Empty<EncounterCoverPosition>();

    public required IReadOnlyList<GridPosition>
        DifficultTerrainPositions { get; init; }
}
