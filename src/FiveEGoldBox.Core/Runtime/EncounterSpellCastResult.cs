using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterSpellCastResult
{
    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string SpellId { get; init; }

    public required int DistanceFeet { get; init; }

    /// Set when the caster rolled to hit.
    public AttackRollResult? AttackRoll { get; init; }

    /// Set when the target rolled to resist.
    public SavingThrowResult? SavingThrow { get; init; }

    /// Whether the spell had any effect on its target at all.
    public required bool TookEffect { get; init; }

    /// Everyone the spell settled on, in the order it reached them.
    public IReadOnlyList<string> EffectedCombatantIds { get; init; }
        = Array.Empty<string>();

    public required int DamageDealt { get; init; }

    public required int HealingDone { get; init; }

    public CombatantDamageResult? TargetDamage { get; init; }

    public required EncounterState State { get; init; }
}
