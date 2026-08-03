using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatView
{
    internal CombatView(
        string encounterId,
        long revision,
        string battlefieldId,
        int battlefieldWidth,
        int battlefieldHeight,
        EncounterLifecycleState lifecycleState,
        int roundNumber,
        string? activeCombatantId,
        string? pendingDeathSavingThrowCombatantId,
        string? winningSideId,
        string partySideId,
        IReadOnlyList<CombatantView> combatants,
        CombatDecision decision)
    {
        if (string.IsNullOrWhiteSpace(encounterId))
        {
            throw new ArgumentException(
                "Encounter ID is required.",
                nameof(encounterId));
        }

        if (string.IsNullOrWhiteSpace(battlefieldId))
        {
            throw new ArgumentException(
                "Battlefield ID is required.",
                nameof(battlefieldId));
        }

        if (string.IsNullOrWhiteSpace(partySideId))
        {
            throw new ArgumentException(
                "Party side ID is required.",
                nameof(partySideId));
        }

        ArgumentNullException.ThrowIfNull(combatants);
        ArgumentNullException.ThrowIfNull(decision);

        CombatantView[] protectedCombatants = combatants.ToArray();

        ValidateShape(
            revision,
            lifecycleState,
            activeCombatantId,
            pendingDeathSavingThrowCombatantId,
            winningSideId,
            decision);

        EncounterId = encounterId;
        Revision = revision;
        BattlefieldId = battlefieldId;
        BattlefieldWidth = battlefieldWidth;
        BattlefieldHeight = battlefieldHeight;
        LifecycleState = lifecycleState;
        RoundNumber = roundNumber;
        ActiveCombatantId = activeCombatantId;
        PendingDeathSavingThrowCombatantId =
            pendingDeathSavingThrowCombatantId;
        WinningSideId = winningSideId;
        PartySideId = partySideId;
        Combatants = Array.AsReadOnly(protectedCombatants);
        Decision = decision;
    }

    public string EncounterId { get; }

    public long Revision { get; }

    public string BattlefieldId { get; }

    public int BattlefieldWidth { get; }

    public int BattlefieldHeight { get; }

    public EncounterLifecycleState LifecycleState { get; }

    public int RoundNumber { get; }

    public string? ActiveCombatantId { get; }

    public string? PendingDeathSavingThrowCombatantId { get; }

    public string? WinningSideId { get; }

    /// Which SideId is the party's, for whichever scenario is running --
    /// the same fact WatchtowerCombatDecisionFactory already resolves
    /// internally (via EncounterPartySideResolver, internal to Application)
    /// to decide whose turn requires a player decision. A client needing
    /// "is this combatant an ally" no longer has to re-derive its own
    /// party-member-ID set from the session to answer it; comparing a
    /// combatant's own SideId against this one is exact, not one client's
    /// approximation of the real mechanism.
    public string PartySideId { get; }

    public IReadOnlyList<CombatantView> Combatants { get; }

    public CombatDecision Decision { get; }

    private static void ValidateShape(
        long revision,
        EncounterLifecycleState lifecycleState,
        string? activeCombatantId,
        string? pendingDeathSavingThrowCombatantId,
        string? winningSideId,
        CombatDecision decision)
    {
        if (lifecycleState == EncounterLifecycleState.Completed)
        {
            if (activeCombatantId is not null
                || pendingDeathSavingThrowCombatantId is not null
                || decision.State != CombatDecisionState.CombatCompleted)
            {
                throw new ArgumentException(
                    "Completed combat view has a contradictory shape.",
                    nameof(lifecycleState));
            }
        }
        else if (string.IsNullOrWhiteSpace(activeCombatantId)
            || winningSideId is not null
            || decision.State == CombatDecisionState.CombatCompleted)
        {
            throw new ArgumentException(
                "Active combat view has a contradictory shape.",
                nameof(lifecycleState));
        }

        if (decision.EncounterRevision != revision)
        {
            throw new ArgumentException(
                "Combat view and decision revisions must agree.",
                nameof(decision));
        }

        if (!string.Equals(
                decision.ActiveCombatantId,
                activeCombatantId,
                StringComparison.Ordinal)
            || !string.Equals(
                decision.PendingDeathSavingThrowCombatantId,
                pendingDeathSavingThrowCombatantId,
                StringComparison.Ordinal)
            || !string.Equals(
                decision.WinningSideId,
                winningSideId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Combat view and decision identity fields must agree.",
                nameof(decision));
        }
    }
}
