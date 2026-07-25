using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal sealed record ExplorationMapDefinition
{
    internal required string MapId { get; init; }

    internal required int Width { get; init; }

    internal required int Height { get; init; }

    internal required ExplorationFloor StartingFloor { get; init; }

    internal required GridPosition StartingPosition { get; init; }

    internal required ExplorationFacing StartingFacing { get; init; }

    /// Squares the party may occupy, per floor. Anything absent is solid.
    internal required IReadOnlyList<ExplorationFloorDefinition> Floors { get; init; }
}
