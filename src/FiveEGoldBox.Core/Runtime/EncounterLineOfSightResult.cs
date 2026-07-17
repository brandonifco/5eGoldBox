namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterLineOfSightResult
{
    public required GridPosition SourcePosition { get; init; }

    public required GridPosition TargetPosition { get; init; }

    public required bool HasLineOfSight { get; init; }

    public GridPosition? BlockingPosition { get; init; }

    public required IReadOnlyList<GridPosition>
        IntermediatePositions
    { get; init; }
}
