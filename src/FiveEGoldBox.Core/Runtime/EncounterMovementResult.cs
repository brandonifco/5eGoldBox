namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterMovementResult
{
    public required string ActorCombatantId { get; init; }

    public required GridPosition StartingPosition { get; init; }

    public required GridPosition EndingPosition { get; init; }

    public required IReadOnlyList<GridPosition> Path { get; init; }

    public required int MovementSpentFeet { get; init; }

    public required EncounterState State { get; init; }
}
