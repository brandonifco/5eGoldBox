namespace FiveEGoldBox.Application.Combat;

/// One discrete thing that happened during a combat operation: a move, an
/// attack, a death saving throw, a turn ending, or the encounter completing.
/// Exactly one of the detail properties is populated, matching Kind.
public sealed record CombatStepResult
{
    internal CombatStepResult(
        CombatStepKind kind,
        long startingEncounterRevision,
        long resultingEncounterRevision,
        string? actorCombatantId,
        string? targetCombatantId,
        IReadOnlyList<CombatDieRoll> dice,
        CombatMovementStepDetail? movement,
        CombatWeaponAttackStepDetail? weaponAttack,
        CombatDeathSavingThrowStepDetail? deathSavingThrow,
        CombatTurnAdvancementStepDetail? turnAdvancement,
        CombatTurnAdvanceReason? turnAdvanceReason,
        string? winningSideId)
    {
        ArgumentNullException.ThrowIfNull(dice);

        Kind = kind;
        StartingEncounterRevision = startingEncounterRevision;
        ResultingEncounterRevision = resultingEncounterRevision;
        ActorCombatantId = actorCombatantId;
        TargetCombatantId = targetCombatantId;
        Dice = Array.AsReadOnly(dice.ToArray());
        Movement = movement;
        WeaponAttack = weaponAttack;
        DeathSavingThrow = deathSavingThrow;
        TurnAdvancement = turnAdvancement;
        TurnAdvanceReason = turnAdvanceReason;
        WinningSideId = winningSideId;
    }

    public CombatStepKind Kind { get; }

    public long StartingEncounterRevision { get; }

    public long ResultingEncounterRevision { get; }

    public string? ActorCombatantId { get; }

    public string? TargetCombatantId { get; }

    /// Dice drawn by this step, in the order the deterministic sequence
    /// produced them. Empty for steps that consume no randomness.
    public IReadOnlyList<CombatDieRoll> Dice { get; }

    public CombatMovementStepDetail? Movement { get; }

    public CombatWeaponAttackStepDetail? WeaponAttack { get; }

    public CombatDeathSavingThrowStepDetail? DeathSavingThrow { get; }

    public CombatTurnAdvancementStepDetail? TurnAdvancement { get; }

    /// Why the turn ended. Populated only for TurnAdvanced steps.
    public CombatTurnAdvanceReason? TurnAdvanceReason { get; }

    /// Set once a step ends the encounter.
    public string? WinningSideId { get; }
}
