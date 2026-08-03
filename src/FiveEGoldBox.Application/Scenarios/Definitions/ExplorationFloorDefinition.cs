using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal sealed record ExplorationFloorDefinition
{
    internal required string Floor { get; init; }

    internal required IReadOnlyList<GridPosition> TraversablePositions { get; init; }

    internal required IReadOnlyList<StairDefinition> Stairs { get; init; }

    internal required IReadOnlyList<DoorDefinition> Doors { get; init; }

    internal required IReadOnlyList<TreasureDefinition> Treasures { get; init; }

    internal required IReadOnlyList<NpcDefinition> Npcs { get; init; }
}
