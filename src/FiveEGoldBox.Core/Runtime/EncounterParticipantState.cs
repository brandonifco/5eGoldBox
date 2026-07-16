using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterParticipantState
{
    public required CombatantState Combatant { get; init; }

    public required EncounterCombatProfile CombatProfile { get; init; }

    public required string SideId { get; init; }

    public required CombatTurnResources TurnResources { get; init; }

    public required GridPosition Position { get; init; }
}
