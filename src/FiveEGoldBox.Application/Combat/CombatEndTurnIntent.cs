namespace FiveEGoldBox.Application.Combat;

public sealed record CombatEndTurnIntent
{
    public required long ExpectedEncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }
}
