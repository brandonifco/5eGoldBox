using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Exploration;

internal static class WatchtowerExplorationSessionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.RegionalTravel is not null)
        {
            throw new ArgumentException(
                "An exploration session cannot contain regional-travel state.",
                nameof(state));
        }

        if (state.ActiveEncounter is not null)
        {
            throw new ArgumentException(
                "An exploration session cannot contain an active encounter.",
                nameof(state));
        }

        ExplorationState exploration =
            state.Exploration
            ?? throw new ArgumentException(
                "An exploration session requires exploration state.",
                nameof(state));

        ScenarioLocationDefinition? location = ScenarioDefinitionRegistry
            .Resolve(state)
            .Locations
            .FirstOrDefault(candidate => string.Equals(
                candidate.LocationId,
                state.CurrentLocationId,
                StringComparison.Ordinal));

        if (location?.ExplorationMap is null)
        {
            throw new ArgumentException(
                "Watchtower exploration requires the ruined-watchtower location.",
                nameof(state));
        }

        if (!location.ExplorableProgressIds.Contains(
            state.Scenario.ProgressId,
            StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Watchtower exploration requires accepted-mission or raiders-defeated progress.",
                nameof(state));
        }

        ScenarioExplorationMap.Validate(
            location.ExplorationMap,
            exploration);
    }
}
