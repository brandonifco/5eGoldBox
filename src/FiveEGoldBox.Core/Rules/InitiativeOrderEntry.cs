namespace FiveEGoldBox.Core.Rules;

public sealed record InitiativeOrderEntry
{
    public required string CombatantId { get; init; }

    public required InitiativeRollResult Initiative { get; init; }

    public required int Position { get; init; }

    public required bool HasTiedInitiative { get; init; }
}