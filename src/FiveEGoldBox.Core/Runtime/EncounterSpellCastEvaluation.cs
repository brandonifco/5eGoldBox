using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

/// What a spell cast would do, worked out from whatever roll it needs before
/// the caller is asked for the dice its effect takes — the same two-phase
/// shape a weapon attack already uses, and for the same reason: a hit or a
/// failed save decides whether effect dice are needed at all, and the caller
/// owns randomness.
internal sealed record EncounterSpellCastEvaluation
{
    public required long EncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string SpellId { get; init; }

    public required EncounterSpellPrerequisiteEvaluation Prerequisites
    { get; init; }

    /// Set only for a spell resolved by an attack roll.
    public AttackRollResult? AttackRoll { get; init; }

    /// Set only for a spell resolved by the target's saving throw.
    public SavingThrowResult? SavingThrow { get; init; }

    public required bool TookEffect { get; init; }

    /// The dice the caller must roll for the spell's own effect, in the order
    /// Resolve will consume them — damage effects first, then healing, each
    /// in declared order. Empty when the spell took no effect.
    public required IReadOnlyList<DieType> RequiredEffectDice { get; init; }

    /// Whether resolving this evaluation would deal the target damage, as
    /// opposed to healing an ally or only applying a buff — what a caller
    /// needs to know to decide whether the target's own concentration check
    /// is potentially in play.
    public required bool WouldDealDamage { get; init; }
}
