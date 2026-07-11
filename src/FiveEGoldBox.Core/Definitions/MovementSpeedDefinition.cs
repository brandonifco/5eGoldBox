using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record MovementSpeedDefinition
{
    public required MovementMode Mode { get; init; }

    public required int SpeedFeet { get; init; }
}
