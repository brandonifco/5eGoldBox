using FiveEGoldBox.Application.Scenarios;
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

        if (!string.Equals(
            state.CurrentLocationId,
            WatchtowerRegionalRoute.WatchtowerLocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Watchtower exploration requires the ruined-watchtower location.",
                nameof(state));
        }

        if (state.Scenario.Progress is not (
            WatchtowerScenarioProgress.MissionAccepted
            or WatchtowerScenarioProgress.RaidersDefeated))
        {
            throw new ArgumentException(
                "Watchtower exploration requires accepted-mission or raiders-defeated progress.",
                nameof(state));
        }

        WatchtowerExplorationMap.Validate(exploration);
    }
}
