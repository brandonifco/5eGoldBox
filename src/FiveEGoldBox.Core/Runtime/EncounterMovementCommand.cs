namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterMovementCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required IReadOnlyList<GridPosition> Path { get; init; }
}
