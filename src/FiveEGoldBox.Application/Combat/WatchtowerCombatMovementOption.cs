using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record WatchtowerCombatMovementOption
{
    public required bool IsAvailable { get; init; }

    public required int MovementRemainingFeet { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }
}
