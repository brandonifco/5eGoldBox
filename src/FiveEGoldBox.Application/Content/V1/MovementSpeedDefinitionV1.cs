namespace FiveEGoldBox.Application.Content.V1;

internal sealed record MovementSpeedDefinitionV1
{
    public required MovementModeV1 Mode { get; init; }

    public required int SpeedFeet { get; init; }
}
