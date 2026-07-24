using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatMovementOption
{
    internal CombatMovementOption(
        bool isAvailable,
        int movementRemainingFeet,
        EncounterActionUnavailabilityReason unavailabilityReason,
        IReadOnlyList<CombatMovementDestinationOption> destinationOptions)
    {
        ArgumentNullException.ThrowIfNull(destinationOptions);

        if (movementRemainingFeet < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(movementRemainingFeet),
                movementRemainingFeet,
                "Remaining movement must not be negative.");
        }

        CombatMovementDestinationOption[] protectedOptions =
            destinationOptions.ToArray();

        if (isAvailable != (protectedOptions.Length > 0))
        {
            throw new ArgumentException(
                "Combat movement availability must match the destination collection.",
                nameof(destinationOptions));
        }

        IsAvailable = isAvailable;
        MovementRemainingFeet = movementRemainingFeet;
        UnavailabilityReason = unavailabilityReason;
        DestinationOptions = Array.AsReadOnly(protectedOptions);
    }

    public bool IsAvailable { get; }

    public int MovementRemainingFeet { get; }

    public EncounterActionUnavailabilityReason UnavailabilityReason { get; }

    public IReadOnlyList<CombatMovementDestinationOption>
        DestinationOptions { get; }
}
