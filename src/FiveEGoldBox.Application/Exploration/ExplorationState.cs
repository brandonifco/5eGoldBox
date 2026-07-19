using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

public sealed record ExplorationState
{
    public required string MapId { get; init; }

    public required ExplorationFloor Floor { get; init; }

    public required GridPosition Position { get; init; }

    public required ExplorationFacing Facing { get; init; }
}
