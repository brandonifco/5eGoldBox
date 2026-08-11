namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterCombatDecision
{
    public required CombatDecisionState State { get; init; }

    public required long EncounterRevision { get; init; }

    public string? ActiveCombatantId { get; init; }

    public string? PendingDeathSavingThrowCombatantId { get; init; }

    public EncounterCombatMovementOption? Movement { get; init; }

    /// One entry per weapon the active combatant carries. A party member with
    /// a bow and a dagger sees both; every NPC in current content has exactly
    /// one, so this is a single-element list for them.
    public IReadOnlyList<EncounterCombatWeaponAttackOption> WeaponAttacks
    { get; init; } = Array.Empty<EncounterCombatWeaponAttackOption>();

    /// One entry per spell the active combatant has prepared. Empty for a
    /// combatant that carries none.
    public IReadOnlyList<EncounterCombatSpellAttackOption> SpellAttacks
    { get; init; } = Array.Empty<EncounterCombatSpellAttackOption>();

    /// Null outside a player decision, exactly like Movement and EndTurn.
    public EncounterCombatDisengageOption? Disengage { get; init; }

    public EncounterCombatEndTurnOption? EndTurn { get; init; }

    public string? WinningSideId { get; init; }
}
