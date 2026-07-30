namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ExplorationMapDefinitionV1
{
    public required string MapId { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required ExplorationFloorV1 StartingFloor { get; init; }

    public required GridPositionV1 StartingPosition { get; init; }

    public required ExplorationFacingV1 StartingFacing { get; init; }

    public required IReadOnlyList<ExplorationFloorDefinitionV1> Floors { get; init; }
}
