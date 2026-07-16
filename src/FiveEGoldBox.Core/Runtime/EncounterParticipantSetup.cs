namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterParticipantSetup
{
    public required CombatantState Combatant { get; init; }

    public required EncounterCombatProfile CombatProfile { get; init; }
    public required string SideId { get; init; }

    public required int MovementSpeedFeet { get; init; }

    public required GridPosition StartingPosition { get; init; }
}
