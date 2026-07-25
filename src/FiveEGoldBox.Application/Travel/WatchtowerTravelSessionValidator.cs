using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Travel;

internal static class WatchtowerTravelSessionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Exploration is not null)
        {
            throw new ArgumentException(
                "A regional-travel session cannot contain exploration state.",
                nameof(state));
        }

        if (state.ActiveEncounter is not null)
        {
            throw new ArgumentException(
                "A regional-travel session cannot contain an active encounter.",
                nameof(state));
        }

        RegionalTravelState travel =
            state.RegionalTravel
            ?? throw new ArgumentException(
                "A regional-travel session requires regional-travel state.",
                nameof(state));

        if (state.Scenario.Progress
            == WatchtowerScenarioProgress.MissionNotAccepted)
        {
            throw new ArgumentException(
                "Regional travel cannot begin before the watchtower mission is accepted.",
                nameof(state));
        }

        if (!string.Equals(
            travel.RouteId,
            WatchtowerRegionalRoute.RouteId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The regional-travel route is unsupported.",
                nameof(state));
        }

        if (!WatchtowerRegionalRoute.HasSupportedEndpoints(
            travel.OriginLocationId,
            travel.DestinationLocationId))
        {
            throw new ArgumentException(
                "The regional-travel endpoints are inconsistent with the watchtower route.",
                nameof(state));
        }

        if (travel.FinalStepIndex
            != WatchtowerRegionalRoute.FinalStepIndex)
        {
            throw new ArgumentException(
                "The regional-travel final step is inconsistent with the watchtower route.",
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
}
