using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatTargetOption
{
    internal CombatTargetOption(
        string targetCombatantId,
        bool isAvailable,
        EncounterActionUnavailabilityReason unavailabilityReason,
        D20RollMode? attackRollMode,
        int? distanceFeet,
        EncounterCoverEvaluation? cover = null,
        Ability? saveAbility = null,
        int? saveDc = null)
    {
        if (string.IsNullOrWhiteSpace(targetCombatantId))
        {
            throw new ArgumentException(
                "Target combatant ID is required.",
                nameof(targetCombatantId));
        }

        if (distanceFeet < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distanceFeet),
                distanceFeet,
                "Target distance must not be negative.");
        }

        TargetCombatantId = targetCombatantId;
        IsAvailable = isAvailable;
        UnavailabilityReason = unavailabilityReason;
        AttackRollMode = attackRollMode;
        DistanceFeet = distanceFeet;
        Cover = cover;
        SaveAbility = saveAbility;
        SaveDc = saveDc;
    }

    public string TargetCombatantId { get; }

    public bool IsAvailable { get; }

    public EncounterActionUnavailabilityReason UnavailabilityReason { get; }

    public D20RollMode? AttackRollMode { get; }

    public int? DistanceFeet { get; }

    /// What this target's position does to the attack before it is made:
    /// the cover level, the armor-class bonus a weapon or spell attack roll
    /// has to beat on top of the target's own AC, and the Dexterity save
    /// bonus a save-resolved spell has to beat. Core has always computed
    /// this per attack (EncounterWeaponAttackPrerequisiteEvaluation.Cover)
    /// and applied it to the real roll; it simply never crossed the
    /// Application boundary, so a client could not warn about it before the
    /// player committed. Null only where the underlying evaluation did not
    /// produce one -- a spell that resolves without either kind of roll.
    public EncounterCoverEvaluation? Cover { get; }

    /// Set only for a spell target resolved by a saving throw.
    public Ability? SaveAbility { get; }

    public int? SaveDc { get; }
}
