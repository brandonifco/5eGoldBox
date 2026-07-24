namespace FiveEGoldBox.Application.Combat;

public sealed record CombatDecision
{
    internal CombatDecision(
        CombatDecisionState state,
        long encounterRevision,
        string? activeCombatantId,
        string? pendingDeathSavingThrowCombatantId,
        CombatMovementOption? movement,
        IReadOnlyList<CombatWeaponAttackOption> weaponAttacks,
        CombatEndTurnOption? endTurn,
        string? winningSideId)
    {
        ArgumentNullException.ThrowIfNull(weaponAttacks);

        CombatWeaponAttackOption[] protectedWeaponAttacks =
            weaponAttacks.ToArray();

        ValidateShape(
            state,
            activeCombatantId,
            pendingDeathSavingThrowCombatantId,
            movement,
            protectedWeaponAttacks,
            endTurn,
            winningSideId);

        State = state;
        EncounterRevision = encounterRevision;
        ActiveCombatantId = activeCombatantId;
        PendingDeathSavingThrowCombatantId =
            pendingDeathSavingThrowCombatantId;
        Movement = movement;
        WeaponAttacks = Array.AsReadOnly(protectedWeaponAttacks);
        EndTurn = endTurn;
        WinningSideId = winningSideId;
    }

    public CombatDecisionState State { get; }

    public long EncounterRevision { get; }

    public string? ActiveCombatantId { get; }

    public string? PendingDeathSavingThrowCombatantId { get; }

    public CombatMovementOption? Movement { get; }

    public IReadOnlyList<CombatWeaponAttackOption> WeaponAttacks { get; }

    public CombatEndTurnOption? EndTurn { get; }

    public string? WinningSideId { get; }

    private static void ValidateShape(
        CombatDecisionState state,
        string? activeCombatantId,
        string? pendingDeathSavingThrowCombatantId,
        CombatMovementOption? movement,
        IReadOnlyList<CombatWeaponAttackOption> weaponAttacks,
        CombatEndTurnOption? endTurn,
        string? winningSideId)
    {
        switch (state)
        {
            case CombatDecisionState.PlayerDecisionRequired:
                if (string.IsNullOrWhiteSpace(activeCombatantId)
                    || pendingDeathSavingThrowCombatantId is not null
                    || movement is null
                    || endTurn is null
                    || winningSideId is not null)
                {
                    throw new ArgumentException(
                        "Player-decision combat output has a contradictory shape.",
                        nameof(state));
                }

                break;
            case CombatDecisionState.AutomaticProcessingRequired:
                if (string.IsNullOrWhiteSpace(activeCombatantId)
                    || movement is not null
                    || weaponAttacks.Count != 0
                    || endTurn is not null
                    || winningSideId is not null)
                {
                    throw new ArgumentException(
                        "Automatic-processing combat output has a contradictory shape.",
                        nameof(state));
                }

                break;
            case CombatDecisionState.CombatCompleted:
                if (activeCombatantId is not null
                    || pendingDeathSavingThrowCombatantId is not null
                    || movement is not null
                    || weaponAttacks.Count != 0
                    || endTurn is not null)
                {
                    throw new ArgumentException(
                        "Completed-combat output has a contradictory shape.",
                        nameof(state));
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(state),
                    state,
                    "Unsupported combat decision state.");
        }
    }
}
