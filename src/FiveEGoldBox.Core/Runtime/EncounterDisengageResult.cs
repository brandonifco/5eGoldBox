namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterDisengageResult
{
    public required string ActorCombatantId { get; init; }

    public required EncounterState State { get; init; }
}
