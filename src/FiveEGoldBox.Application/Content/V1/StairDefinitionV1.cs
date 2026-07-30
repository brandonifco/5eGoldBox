namespace FiveEGoldBox.Application.Content.V1;

internal sealed record StairDefinitionV1
{
    public required GridPositionV1 Position { get; init; }

    public required string DestinationFloor { get; init; }

    public required GridPositionV1 DestinationPosition { get; init; }
}
