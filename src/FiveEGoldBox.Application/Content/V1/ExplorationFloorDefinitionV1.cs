namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ExplorationFloorDefinitionV1
{
    public required string Floor { get; init; }

    public required IReadOnlyList<GridPositionV1> TraversablePositions { get; init; }

    public required IReadOnlyList<StairDefinitionV1> Stairs { get; init; }
}
