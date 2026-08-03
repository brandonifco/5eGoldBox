using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

public sealed record ExplorationState
{
    public required string MapId { get; init; }

    public required string Floor { get; init; }

    public required GridPosition Position { get; init; }

    public required ExplorationFacing Facing { get; init; }

    public IReadOnlyList<string> RevealedSecretDoorIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> CollectedTreasureIds { get; init; } = Array.Empty<string>();
}
