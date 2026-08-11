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
        IReadOnlyList<CombatSpellAttackOption> spellAttacks,
        CombatDisengageOption? disengage,
        CombatEndTurnOption? endTurn,
        string? winningSideId)
    {
        ArgumentNullException.ThrowIfNull(weaponAttacks);
        ArgumentNullException.ThrowIfNull(spellAttacks);

        CombatWeaponAttackOption[] protectedWeaponAttacks =
            weaponAttacks.ToArray();
        CombatSpellAttackOption[] protectedSpellAttacks =
            spellAttacks.ToArray();

        ValidateShape(
            state,
            activeCombatantId,
            pendingDeathSavingThrowCombatantId,
            movement,
            protectedWeaponAttacks,
            protectedSpellAttacks,
            disengage,
            endTurn,
            winningSideId);

        State = state;
        EncounterRevision = encounterRevision;
        ActiveCombatantId = activeCombatantId;
        PendingDeathSavingThrowCombatantId =
            pendingDeathSavingThrowCombatantId;
        Movement = movement;
        WeaponAttacks = Array.AsReadOnly(protectedWeaponAttacks);
        SpellAttacks = Array.AsReadOnly(protectedSpellAttacks);
        Disengage = disengage;
        EndTurn = endTurn;
        WinningSideId = winningSideId;
    }

    public CombatDecisionState State { get; }

    public long EncounterRevision { get; }

    public string? ActiveCombatantId { get; }

    public string? PendingDeathSavingThrowCombatantId { get; }

    public CombatMovementOption? Movement { get; }

    public IReadOnlyList<CombatWeaponAttackOption> WeaponAttacks { get; }

    public IReadOnlyList<CombatSpellAttackOption> SpellAttacks { get; }

    /// Present exactly when a player decision is required, alongside
    /// Movement and EndTurn. Its own IsAvailable says whether the action
    /// is actually still affordable this turn.
    public CombatDisengageOption? Disengage { get; }

    public CombatEndTurnOption? EndTurn { get; }

    public string? WinningSideId { get; }

    private static void ValidateShape(
        CombatDecisionState state,
        string? activeCombatantId,
        string? pendingDeathSavingThrowCombatantId,
        CombatMovementOption? movement,
        IReadOnlyList<CombatWeaponAttackOption> weaponAttacks,
        IReadOnlyList<CombatSpellAttackOption> spellAttacks,
        CombatDisengageOption? disengage,
        CombatEndTurnOption? endTurn,
        string? winningSideId)
    {
        switch (state)
        {
            case CombatDecisionState.PlayerDecisionRequired:
                if (string.IsNullOrWhiteSpace(activeCombatantId)
                    || pendingDeathSavingThrowCombatantId is not null
                    || movement is null
                    || disengage is null
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
                    || spellAttacks.Count != 0
                    || disengage is not null
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
                    || spellAttacks.Count != 0
                    || disengage is not null
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
