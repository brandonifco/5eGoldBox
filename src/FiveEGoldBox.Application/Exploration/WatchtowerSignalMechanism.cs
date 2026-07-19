using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

internal static class WatchtowerSignalMechanism
{
    internal const ExplorationFloor Floor =
        ExplorationFloor.UpperFloor;

    internal const ExplorationFacing RequiredFacing =
        ExplorationFacing.East;

    internal static readonly GridPosition
        InteractionPosition = new(1, 1);

    internal static bool CanActivate(
        ExplorationState exploration)
    {
        ArgumentNullException.ThrowIfNull(exploration);

        return string.Equals(
                exploration.MapId,
                WatchtowerExplorationMap.MapId,
                StringComparison.Ordinal)
            && exploration.Floor == Floor
            && exploration.Position
                == InteractionPosition
            && exploration.Facing
                == RequiredFacing;
    }
}
