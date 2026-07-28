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
        SaveAbility = saveAbility;
        SaveDc = saveDc;
    }

    public string TargetCombatantId { get; }

    public bool IsAvailable { get; }

    public EncounterActionUnavailabilityReason UnavailabilityReason { get; }

    public D20RollMode? AttackRollMode { get; }

    public int? DistanceFeet { get; }

    /// Set only for a spell target resolved by a saving throw.
    public Ability? SaveAbility { get; }

    public int? SaveDc { get; }
}
