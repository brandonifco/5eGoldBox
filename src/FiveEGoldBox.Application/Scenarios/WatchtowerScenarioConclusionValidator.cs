using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Scenarios;

internal static class WatchtowerScenarioConclusionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!string.Equals(
            state.CurrentLocationId,
            WatchtowerRegionalRoute.WatchtowerLocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The watchtower defeat conclusion must remain at the ruined watchtower.",
                nameof(state));
        }

        if (state.Scenario.Progress
            != WatchtowerScenarioProgress.PartyDefeated)
        {
            throw new ArgumentException(
                "The watchtower scenario conclusion requires party-defeated progress.",
                nameof(state));
        }

        if (state.RegionalTravel is not null)
        {
            throw new ArgumentException(
                "A watchtower scenario conclusion cannot contain regional-travel state.",
                nameof(state));
        }

        if (state.Exploration is not null)
        {
            throw new ArgumentException(
                "A watchtower scenario conclusion cannot contain exploration state.",
                nameof(state));
        }

        if (state.ActiveEncounter is not null)
        {
            throw new ArgumentException(
                "A watchtower scenario conclusion cannot contain an active encounter.",
                nameof(state));
        }
    }
}
