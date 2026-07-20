namespace FiveEGoldBox.Application.Combat;

public sealed record WatchtowerCombatDecision
{
    public required WatchtowerCombatDecisionState State { get; init; }

    public required long EncounterRevision { get; init; }

    public string? ActiveCombatantId { get; init; }

    public string? PendingDeathSavingThrowCombatantId { get; init; }

    public WatchtowerCombatMovementOption? Movement { get; init; }

    public WatchtowerCombatWeaponAttackOption? WeaponAttack { get; init; }

    public WatchtowerCombatEndTurnOption? EndTurn { get; init; }

    public string? WinningSideId { get; init; }
}
