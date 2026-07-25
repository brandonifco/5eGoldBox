using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal sealed record ExplorationFloorDefinition
{
    internal required ExplorationFloor Floor { get; init; }

    internal required IReadOnlyList<GridPosition> TraversablePositions { get; init; }

    internal required IReadOnlyList<StairDefinition> Stairs { get; init; }
}
