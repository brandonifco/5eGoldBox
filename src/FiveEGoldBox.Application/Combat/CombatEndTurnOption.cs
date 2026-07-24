using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatEndTurnOption
{
    internal CombatEndTurnOption(
        bool isAvailable,
        EncounterActionUnavailabilityReason unavailabilityReason)
    {
        IsAvailable = isAvailable;
        UnavailabilityReason = unavailabilityReason;
    }

    public bool IsAvailable { get; }

    public EncounterActionUnavailabilityReason UnavailabilityReason { get; }
}
