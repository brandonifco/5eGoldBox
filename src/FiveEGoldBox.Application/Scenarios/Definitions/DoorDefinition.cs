using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A door sits on one edge of Position's tile -- the side named by Side --
/// connecting it to the adjacent tile on that side, rather than occupying a
/// tile of its own. Movement between the two connected tiles is blocked
/// until the door is opened; a secret door additionally requires being
/// found (revealed) before it can be opened at all. A locked door has no
/// unlock path in v1 -- it is a permanent binary blocker, not a puzzle with
/// a key waiting to be authored later.
internal sealed record DoorDefinition
{
    internal required string DoorId { get; init; }

    internal required GridPosition Position { get; init; }

    internal required ExplorationFacing Side { get; init; }

    internal required bool IsSecret { get; init; }

    internal required bool IsLocked { get; init; }

    /// The tile on the other side of the door from Position.
    internal GridPosition OtherPosition =>
        ExplorationFacingOffsets.Apply(Side, Position);

    /// True if this door's edge is the one between the two given tiles,
    /// checked in either order since neither tile is privileged.
    internal bool ConnectsPositions(GridPosition a, GridPosition b)
    {
        return (Position == a && OtherPosition == b)
            || (Position == b && OtherPosition == a);
    }
}
