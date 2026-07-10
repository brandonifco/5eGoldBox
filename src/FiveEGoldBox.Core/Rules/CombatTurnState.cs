namespace FiveEGoldBox.Core.Rules;

public sealed record CombatTurnState
{
    public required IReadOnlyList<InitiativeOrderEntry> InitiativeOrder { get; init; }

    public required int RoundNumber { get; init; }

    public required int ActivePosition { get; init; }
}