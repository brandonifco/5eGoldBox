namespace FiveEGoldBox.Application.Combat;

/// Who finished their turn and who acts next.
public sealed record CombatTurnAdvancementStepDetail
{
    internal CombatTurnAdvancementStepDetail(
        string endedTurnCombatantId,
        string activeCombatantId,
        int previousRoundNumber,
        int roundNumber,
        bool startedNewRound,
        IReadOnlyList<string> skippedCombatantIds)
    {
        ArgumentNullException.ThrowIfNull(skippedCombatantIds);

        EndedTurnCombatantId = endedTurnCombatantId;
        ActiveCombatantId = activeCombatantId;
        PreviousRoundNumber = previousRoundNumber;
        RoundNumber = roundNumber;
        StartedNewRound = startedNewRound;
        SkippedCombatantIds =
            Array.AsReadOnly(skippedCombatantIds.ToArray());
    }

    public string EndedTurnCombatantId { get; }

    public string ActiveCombatantId { get; }

    public int PreviousRoundNumber { get; }

    public int RoundNumber { get; }

    public bool StartedNewRound { get; }

    /// Combatants passed over because they could not act.
    public IReadOnlyList<string> SkippedCombatantIds { get; }
}
