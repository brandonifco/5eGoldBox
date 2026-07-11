namespace FiveEGoldBox.Core.Rules;

public sealed record InitiativeOrderCombatant
{
    public required string CombatantId { get; init; }

    public required InitiativeRollResult Initiative { get; init; }
}
