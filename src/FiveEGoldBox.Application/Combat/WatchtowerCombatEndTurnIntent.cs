namespace FiveEGoldBox.Application.Combat;

internal sealed record WatchtowerCombatEndTurnIntent
{
    public required long ExpectedEncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }
}
