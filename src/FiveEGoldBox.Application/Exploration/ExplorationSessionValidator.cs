using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Exploration;

internal static class ExplorationSessionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        SessionSubstateInvariant.RequireAbsent(
            state,
            state.RegionalTravel,
            "An exploration session cannot contain regional-travel state.");

        SessionSubstateInvariant.RequireAbsent(
            state,
            state.ActiveEncounter,
            "An exploration session cannot contain an active encounter.");

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
                "Exploration requires a location the scenario gave a map.",
                nameof(state));
        }

        if (!location.ExplorableProgressIds.Contains(
            state.Scenario.ProgressId,
            StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Exploration here requires progress the scenario opens this map at.",
                nameof(state));
        }

        ScenarioExplorationMap.Validate(
            location.ExplorationMap,
            exploration);
    }
}
