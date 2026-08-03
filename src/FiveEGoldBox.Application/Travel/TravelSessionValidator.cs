using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Travel;

internal static class TravelSessionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        SessionSubstateInvariant.RequireAbsent(
            state,
            state.Exploration,
            "A regional-travel session cannot contain exploration state.");

        SessionSubstateInvariant.RequireAbsent(
            state,
            state.ActiveEncounter,
            "A regional-travel session cannot contain an active encounter.");

        RegionalTravelState travel =
            state.RegionalTravel
            ?? throw new ArgumentException(
                "A regional-travel session requires regional-travel state.",
                nameof(state));

        // The route is authored content, so it is read from the scenario the
        // session is running rather than from a constant.
        TravelRouteDefinition? route = ScenarioDefinitionRegistry
            .Resolve(state)
            .Routes
            .FirstOrDefault(candidate => string.Equals(
                candidate.RouteId,
                travel.RouteId,
                StringComparison.Ordinal));

        if (route is null)
        {
            throw new ArgumentException(
                "The regional-travel route is unsupported.",
                nameof(state));
        }

        // Being on a road the scenario has not opened yet is the generic form
        // of setting out before accepting the job.
        if (!route.RequiredProgressIds.Contains(
            state.Scenario.ProgressId,
            StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Regional travel cannot begin before the route's required progress is made.",
                nameof(state));
        }

        if (!HasEndpoints(route, travel))
        {
            throw new ArgumentException(
                "The regional-travel endpoints are inconsistent with the authored route.",
                nameof(state));
        }

        if (travel.FinalStepIndex != route.FinalStepIndex)
        {
            throw new ArgumentException(
                "The regional-travel final step is inconsistent with the authored route.",
                nameof(state));
        }

        if (travel.CurrentStepIndex < 0
            || travel.CurrentStepIndex
                > travel.FinalStepIndex)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                travel.CurrentStepIndex,
                "The regional-travel current step must be within the fixed route.");
        }

        string expectedLocationId = travel.IsComplete
            ? travel.DestinationLocationId
            : travel.OriginLocationId;

        if (!string.Equals(
            state.CurrentLocationId,
            expectedLocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The current location is inconsistent with regional-travel progress.",
                nameof(state));
        }
    }

    private static bool HasEndpoints(
        TravelRouteDefinition route,
        RegionalTravelState travel)
    {
        return (string.Equals(
                    travel.OriginLocationId,
                    route.OriginLocationId,
                    StringComparison.Ordinal)
                && string.Equals(
                    travel.DestinationLocationId,
                    route.DestinationLocationId,
                    StringComparison.Ordinal))
            || (string.Equals(
                    travel.OriginLocationId,
                    route.DestinationLocationId,
                    StringComparison.Ordinal)
                && string.Equals(
                    travel.DestinationLocationId,
                    route.OriginLocationId,
                    StringComparison.Ordinal));
    }
}
