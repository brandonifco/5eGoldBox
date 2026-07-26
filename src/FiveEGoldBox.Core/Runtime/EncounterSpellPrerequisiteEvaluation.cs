using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterSpellPrerequisiteEvaluation
{
    public required bool IsLegal { get; init; }

    public required EncounterActionUnavailabilityReason
        UnavailabilityReason { get; init; }

    /// Set only for a spell resolved by an attack roll. A spell resisted by a
    /// saving throw, or one that simply lands, has no roll mode because the
    /// caster rolls nothing.
    public required D20RollMode? AttackRollMode { get; init; }

    public required int? DistanceFeet { get; init; }

    public required EncounterLineOfSightResult? LineOfSight { get; init; }

    /// What the caster's effects add to a spell attack roll. A blessed cleric
    /// aims a Sacred Flame no better — the target rolls that one — but a
    /// blessed wizard's Fire Bolt is a d4 likelier to land.
    public RollContributionSet AttackRollContributions { get; init; } =
        RollContributionSet.None(
            RollContributionTarget.AttackRoll);

    /// What this target's own effects add to the saving throw it makes against
    /// the spell. Resolved here because the caller has to roll those dice
    /// before it can hand the save in.
    public RollContributionSet SavingThrowContributions { get; init; } =
        RollContributionSet.None(
            RollContributionTarget.SavingThrow);

    public EncounterCoverEvaluation? Cover { get; init; }
}
