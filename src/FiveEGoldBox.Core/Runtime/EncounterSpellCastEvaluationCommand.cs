namespace FiveEGoldBox.Core.Runtime;

/// Deliberately carries no AdditionalTargetCombatantIds, unlike
/// EncounterSpellCastCommand. RequiredEffectDice and WouldDealDamage are
/// derived from the primary target alone — correct today, since multi-target
/// casting only reaches effect-applying spells (ApplyEffect already touches
/// every named target) and never a damage or healing one (ResolveDamage and
/// ResolveHealing both land their whole total on the primary target
/// regardless of how many are named). A damage spell that actually split its
/// effect per target would need this evaluated per target too — not just
/// this command extended, but ResolveDamage/ResolveHealing rewritten to stop
/// assuming a single target.
internal sealed record EncounterSpellCastEvaluationCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string SpellId { get; init; }

    public int? FirstAttackRoll { get; init; }

    public int? SecondAttackRoll { get; init; }

    public IReadOnlyList<int> AttackContributionRolls { get; init; }
        = Array.Empty<int>();

    public int? SavingThrowRoll { get; init; }

    public IReadOnlyList<int> SavingThrowContributionRolls { get; init; }
        = Array.Empty<int>();
}
