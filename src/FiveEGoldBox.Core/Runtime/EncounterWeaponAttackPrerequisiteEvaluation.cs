using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

internal sealed record EncounterWeaponAttackPrerequisiteEvaluation
{
    public required bool IsLegal { get; init; }

    public required EncounterActionUnavailabilityReason
        UnavailabilityReason { get; init; }

    public required D20RollMode? AttackRollMode { get; init; }

    public required int? DistanceFeet { get; init; }

    public required EncounterLineOfSightResult?
        LineOfSight { get; init; }

    /// What the attacker's effects add to the attack roll, and the dice the
    /// caller must roll for them before the attack can be resolved.
    public RollContributionSet AttackRollContributions { get; init; } =
        RollContributionSet.None(
            RollContributionTarget.AttackRoll);

    /// What this attack looks like to a contribution that only counts some of
    /// the time. Worked out once here so the attack roll and the damage roll
    /// cannot disagree about it. Null when the attack is not legal, because
    /// there is then no attack to describe.
    public RollContributionContext? ContributionContext { get; init; }

    public EncounterCoverEvaluation Cover { get; init; } =
        new()
        {
            CoverLevel = EncounterCoverLevel.None,
            ArmorClassBonus = 0,
            DexteritySavingThrowBonus = 0,
            CoverPosition = null
        };
}
