using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Whether the active combatant can take the Disengage action: spend the
/// Action so the rest of this turn's movement provokes no opportunity
/// attacks.
///
/// Offered as its own option rather than folded into the movement option,
/// because it is a decision taken *before* choosing where to go — the whole
/// point is to change what a subsequent move costs.
public sealed record CombatDisengageOption
{
    internal CombatDisengageOption(
        bool isAvailable,
        EncounterActionUnavailabilityReason unavailabilityReason)
    {
        IsAvailable = isAvailable;
        UnavailabilityReason = unavailabilityReason;
    }

    public bool IsAvailable { get; }

    public EncounterActionUnavailabilityReason UnavailabilityReason { get; }
}
