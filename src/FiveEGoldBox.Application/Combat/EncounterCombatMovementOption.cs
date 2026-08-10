using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterCombatMovementOption
{
    public required bool IsAvailable { get; init; }

    public required int MovementRemainingFeet { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }

    public IReadOnlyList<EncounterCombatMovementDestinationOption>
        DestinationOptions { get; init; }
            = Array.Empty<EncounterCombatMovementDestinationOption>();
}
