namespace FiveEGoldBox.Application.Combat;

internal sealed record WatchtowerCombatDecision
{
    public required WatchtowerCombatDecisionState State { get; init; }

    public required long EncounterRevision { get; init; }

    public string? ActiveCombatantId { get; init; }

    public string? PendingDeathSavingThrowCombatantId { get; init; }

    public WatchtowerCombatMovementOption? Movement { get; init; }

    /// One entry per weapon the active combatant carries. A party member with
    /// a bow and a dagger sees both; every NPC in current content has exactly
    /// one, so this is a single-element list for them.
    public IReadOnlyList<WatchtowerCombatWeaponAttackOption> WeaponAttacks
    { get; init; } = Array.Empty<WatchtowerCombatWeaponAttackOption>();

    public WatchtowerCombatEndTurnOption? EndTurn { get; init; }

    public string? WinningSideId { get; init; }
}
