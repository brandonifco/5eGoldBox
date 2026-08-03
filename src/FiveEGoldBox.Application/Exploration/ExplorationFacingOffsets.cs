using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

/// The single place a facing/side direction becomes a grid offset --
/// shared by ExplorationRules' own forward-movement math and
/// DoorDefinition's edge (the tile on the other side of a door).
internal static class ExplorationFacingOffsets
{
    internal static GridPosition Apply(
        ExplorationFacing facing,
        GridPosition position)
    {
        return facing switch
        {
            ExplorationFacing.North =>
                position with { Y = position.Y - 1 },
            ExplorationFacing.East =>
                position with { X = position.X + 1 },
            ExplorationFacing.South =>
                position with { Y = position.Y + 1 },
            ExplorationFacing.West =>
                position with { X = position.X - 1 },
            _ => throw new ArgumentOutOfRangeException(
                nameof(facing),
                facing,
                "Unsupported exploration facing.")
        };
    }
}
