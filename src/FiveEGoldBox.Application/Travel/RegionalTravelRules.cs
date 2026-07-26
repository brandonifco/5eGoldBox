using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Travel;

public static class RegionalTravelRules
{
    public static bool CanBeginWatchtowerJourney(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            return false;
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        return GetBeginAvailability(canonicalSession)
            == BeginAvailability.Available;
    }

    public static ApplicationSessionState
        BeginWatchtowerJourney(
            ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        switch (GetBeginAvailability(canonicalSession))
        {
            case BeginAvailability.Available:
                break;
            case BeginAvailability.WrongMode:
                throw new InvalidOperationException(
                    "A watchtower journey may begin only from the outpost.");
            case BeginAvailability.WrongProgress:
                throw new InvalidOperationException(
                    "A watchtower journey may begin only after the mission is accepted and before later scenario progress.");
            case BeginAvailability.WrongLocation:
                throw new InvalidOperationException(
                    "A watchtower journey may begin only from the canonical outpost location.");
            default:
                throw new InvalidOperationException(
                    "The watchtower journey availability could not be resolved.");
        }

        TravelRouteDefinition route = ScenarioDefinitionRegistry
            .Resolve(canonicalSession)
            .Routes
            .Single();

        RegionalTravelState travel = new()
        {
            RouteId = route.RouteId,
            OriginLocationId =
                canonicalSession.CurrentLocationId,
            DestinationLocationId = route.DestinationLocationId,
            CurrentStepIndex = 0,
            FinalStepIndex = route.FinalStepIndex
        };

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                CurrentMode =
                    ApplicationMode.RegionalTravel,
                RegionalTravel = travel
            });
    }

    public static bool CanAdvance(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CurrentMode
            != ApplicationMode.RegionalTravel)
        {
            return false;
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        return GetAdvanceAvailability(canonicalSession)
            == AdvanceAvailability.Available;
    }

    public static RegionalTravelAdvanceResult Advance(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        switch (GetAdvanceAvailability(canonicalSession))
        {
            case AdvanceAvailability.Available:
                break;
            case AdvanceAvailability.WrongMode:
                throw new InvalidOperationException(
                    "Regional travel can advance only while the session is in regional-travel mode.");
            case AdvanceAvailability.WrongProgress:
                throw new InvalidOperationException(
                    "The outbound watchtower journey can advance only while the accepted mission is active.");
            case AdvanceAvailability.Complete:
                throw new InvalidOperationException(
                    "A completed regional journey cannot advance again.");
            default:
                throw new InvalidOperationException(
                    "The regional-travel advance availability could not be resolved.");
        }

        RegionalTravelState travel =
            canonicalSession.RegionalTravel!;
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

    private static BeginAvailability GetBeginAvailability(
        ApplicationSessionState session)
    {
        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            return BeginAvailability.WrongMode;
        }

        if (!IsRouteOpen(session))
        {
            return BeginAvailability.WrongProgress;
        }

        if (!string.Equals(
            session.CurrentLocationId,
            ScenarioDefinitionRegistry
                .Resolve(session)
                .StartingLocationId,
            StringComparison.Ordinal))
        {
            return BeginAvailability.WrongLocation;
        }

        return BeginAvailability.Available;
    }

    private static AdvanceAvailability GetAdvanceAvailability(
        ApplicationSessionState session)
    {
        if (session.CurrentMode
            != ApplicationMode.RegionalTravel)
        {
            return AdvanceAvailability.WrongMode;
        }

        if (!IsRouteOpen(session))
        {
            return AdvanceAvailability.WrongProgress;
        }

        RegionalTravelState travel =
            session.RegionalTravel!;

        return travel.IsComplete
            ? AdvanceAvailability.Complete
            : AdvanceAvailability.Available;
    }

    private enum BeginAvailability
    {
        Available = 0,
        WrongMode = 1,
        WrongProgress = 2,
        WrongLocation = 3
    }

    private enum AdvanceAvailability
    {
        Available = 0,
        WrongMode = 1,
        WrongProgress = 2,
        Complete = 3
    }

    /// A route opens once the party has made the progress it asks for.
    private static bool IsRouteOpen(
        ApplicationSessionState session)
    {
        return ScenarioDefinitionRegistry
            .Resolve(session)
            .Routes
            .Any(route => route.RequiredProgressIds.Contains(
                session.Scenario.ProgressId,
                StringComparer.Ordinal));
    }
}
