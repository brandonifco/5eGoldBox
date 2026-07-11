using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed record CharacterMovementSpeed
{
    public required MovementMode Mode { get; init; }

    public required int SpeedFeet { get; init; }
}
