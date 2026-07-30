using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

/// Flattens an ExplorationMapDefinition (internal content) plus a live
/// ExplorationState into the public, client-safe ExplorationMapView —
/// the same role CombatViewFactory plays for CombatView.
internal static class ExplorationMapViewFactory
{
    internal static ExplorationMapView Create(
        ExplorationMapDefinition map,
        ExplorationState state)
    {
        ExplorationFloorDefinition floor = map.Floors
            .First(candidate => string.Equals(
                candidate.Floor,
                state.Floor,
                StringComparison.Ordinal));

        return new ExplorationMapView(
            map.MapId,
            state.Floor,
            map.Width,
            map.Height,
            floor.TraversablePositions,
            floor.Stairs
                .Select(stair => stair.Position)
                .ToArray(),
            state.Position,
            state.Facing);
    }
}
