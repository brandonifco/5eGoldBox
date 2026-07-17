namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterTurnAdvancementResult
{
    public required string EndedTurnCombatantId { get; init; }

    public required string ActiveCombatantId { get; init; }

    public required int PreviousRoundNumber { get; init; }

    public required int RoundNumber { get; init; }

    public required int PreviousActivePosition { get; init; }

    public required int ActivePosition { get; init; }

    public required IReadOnlyList<string>
        SkippedCombatantIds
    { get; init; }

    public required EncounterState State { get; init; }

    public bool StartedNewRound =>
        RoundNumber > PreviousRoundNumber;
}
