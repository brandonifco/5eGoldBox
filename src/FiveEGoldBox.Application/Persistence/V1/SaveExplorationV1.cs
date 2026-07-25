namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveExplorationV1
{
    public required string MapId { get; init; }

    public required SaveExplorationFloorV1 Floor { get; init; }

    public required SaveGridPositionV1 Position { get; init; }

    public required SaveExplorationFacingV1 Facing { get; init; }
}
