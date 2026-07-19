using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Travel;

public static class RegionalTravelRules
{
    public static ApplicationSessionState
        BeginWatchtowerJourney(
            ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.CurrentMode
            != ApplicationMode.Outpost)
        {
            throw new InvalidOperationException(
                "A watchtower journey may begin only from the outpost.");
        }

        if (canonicalSession.Scenario.Progress
            != WatchtowerScenarioProgress.MissionAccepted)
        {
            throw new InvalidOperationException(
                "A watchtower journey may begin only after the mission is accepted and before later scenario progress.");
        }

        RegionalTravelState travel = new()
        {
            RouteId = WatchtowerRegionalRoute.RouteId,
            OriginLocationId =
                canonicalSession.CurrentLocationId,
            DestinationLocationId =
                WatchtowerRegionalRoute
                    .WatchtowerLocationId,
            CurrentStepIndex = 0,
            FinalStepIndex =
                WatchtowerRegionalRoute.FinalStepIndex
        };

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                CurrentMode =
                    ApplicationMode.RegionalTravel,
                RegionalTravel = travel
            });
    }

    public static RegionalTravelAdvanceResult Advance(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.CurrentMode
            != ApplicationMode.RegionalTravel)
        {
            throw new InvalidOperationException(
                "Regional travel can advance only while the session is in regional-travel mode.");
        }

        RegionalTravelState travel =
            canonicalSession.RegionalTravel!;

        if (travel.IsComplete)
        {
            throw new InvalidOperationException(
                "A completed regional journey cannot advance again.");
        }

        int nextStepIndex =
            travel.CurrentStepIndex + 1;
        bool didArrive =
            nextStepIndex == travel.FinalStepIndex;

        ApplicationSessionState advancedSession =
            ApplicationSessionRules.CreateCanonical(
                canonicalSession with
                {
                    CurrentLocationId = didArrive
                        ? travel.DestinationLocationId
                        : canonicalSession
                            .CurrentLocationId,
                    RegionalTravel = travel with
                    {
                        CurrentStepIndex = nextStepIndex
                    }
                });

        return new RegionalTravelAdvanceResult
        {
            DidArrive = didArrive,
            State = advancedSession
        };
    }
}
