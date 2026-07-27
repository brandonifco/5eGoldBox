namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterWeaponAttackCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string WeaponId { get; init; }

    public required int FirstAttackRoll { get; init; }

    public int? SecondAttackRoll { get; init; }

    /// The dice the attacker's effects added to the attack roll, in the order
    /// the prerequisite evaluation asked for them. Empty for an attacker
    /// nothing is contributing to.
    public IReadOnlyList<int> ContributionRolls { get; init; }
        = Array.Empty<int>();

    public required IReadOnlyList<int>
        DamageRolls
    { get; init; }

    /// The dice the attacker's own features and effects added to the damage,
    /// in the order the evaluation asked for them. Sneak Attack is the first
    /// of these.
    public IReadOnlyList<int> DamageContributionRolls { get; init; }
        = Array.Empty<int>();
}
