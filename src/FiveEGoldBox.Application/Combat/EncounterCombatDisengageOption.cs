using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterCombatDisengageOption
{
    public required bool IsAvailable { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }
}
