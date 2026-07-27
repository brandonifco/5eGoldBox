using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

/// What the roll being made looks like, for contributions that only count
/// under some circumstances.
///
/// Worked out once by whoever is resolving the roll and handed to every
/// gather, so that the attack roll and the damage roll of one attack cannot
/// disagree about whether the attacker had the upper hand.
public sealed record RollContributionContext
{
    public required D20RollMode AttackRollMode { get; init; }

    /// Whether the target has a conscious enemy of its own next to it,
    /// counting the attacker's allies but not the attacker.
    public required bool TargetHasAdjacentEnemy { get; init; }

    public required bool WeaponIsFinesseOrRanged { get; init; }
}
