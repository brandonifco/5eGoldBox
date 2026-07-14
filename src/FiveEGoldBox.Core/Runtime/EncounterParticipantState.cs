namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterParticipantState
{
    public required CombatantState Combatant { get; init; }

    public required string SideId { get; init; }
}
