using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Travel;

public static class RegionalTravelRules
{
    public static bool CanBeginJourney(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            return false;
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        return GetAvailableRoutes(canonicalSession).Count > 0;
    }

    /// Every route open to the party from exactly where it stands right now.
    /// Usually one, but a location with more than one road out — a choice of
    /// approach, a fork — offers more than one, and it is the party's choice
    /// which to take, not the engine's.
    ///
    /// Empty outside the outpost: a route only ever begins from a hub, the
    /// same restriction <see cref="BeginJourney"/> enforces. A location the
    /// party reaches by road does not, on its own, become a place further
    /// roads can start from — that would need a way back into a hub-like
    /// mode from exploration, which nothing today provides.
    ///
    /// Internal rather than a public read model: a client discovers routes
    /// through <see cref="FiveEGoldBox.Application.Views.SessionView"/>'s
    /// per-route <see cref="FiveEGoldBox.Application.Views.SessionAction"/>
    /// entries, the same way it discovers every other choice, rather than
    /// calling here directly.
    internal static IReadOnlyList<TravelRouteDefinition> GetAvailableRoutes(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            return Array.Empty<TravelRouteDefinition>();
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        return ScenarioDefinitionRegistry
            .Resolve(canonicalSession)
            .Routes
            .Where(route => string.Equals(
                    route.OriginLocationId,
                    canonicalSession.CurrentLocationId,
                    StringComparison.Ordinal)
                && route.RequiredProgressIds.Contains(
                    canonicalSession.Scenario.ProgressId,
                    StringComparer.Ordinal))
            .ToArray();
    }

    /// Begins the journey along <paramref name="routeId"/>. May be omitted
    /// when exactly one route is open from here; naming one is required the
    /// moment there is a choice to make.
    public static ApplicationSessionState
        BeginJourney(
            ApplicationSessionState session,
            string? routeId = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.CurrentMode != ApplicationMode.Outpost)
        {
            throw new InvalidOperationException(
                "A journey may begin only from the outpost.");
        }

        TravelRouteDefinition route =
            ResolveRoute(canonicalSession, routeId);

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
                    "The outbound journey can advance only while its route remains open.");
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

    /// Picks the route a journey should follow: the one named, or the only
    /// one open when nothing was named.
    private static TravelRouteDefinition ResolveRoute(
        ApplicationSessionState session,
        string? routeId)
    {
        IReadOnlyList<TravelRouteDefinition> available =
            GetAvailableRoutes(session);

        if (routeId is not null)
        {
            return available.FirstOrDefault(route => string.Equals(
                    route.RouteId,
                    routeId,
                    StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    $"Route '{routeId}' is not open from here.");
        }

        return available.Count switch
        {
            0 => throw new InvalidOperationException(
                "No route is open from here."),
            1 => available[0],
            _ => throw new InvalidOperationException(
                "More than one route is open from here; a route ID is required to choose between them.")
        };
    }

    private static AdvanceAvailability GetAdvanceAvailability(
        ApplicationSessionState session)
    {
        if (session.CurrentMode
            != ApplicationMode.RegionalTravel)
        {
            return AdvanceAvailability.WrongMode;
        }

        RegionalTravelState travel =
            session.RegionalTravel!;

        if (!IsActiveRouteOpen(session, travel))
        {
            return AdvanceAvailability.WrongProgress;
        }

        return travel.IsComplete
            ? AdvanceAvailability.Complete
            : AdvanceAvailability.Available;
    }

    /// Whether the specific route a journey is following remains open — not
    /// merely whether some other route the scenario declares happens to be,
    /// which is what this checked before more than one could exist.
    private static bool IsActiveRouteOpen(
        ApplicationSessionState session,
        RegionalTravelState travel)
    {
        return ScenarioDefinitionRegistry
            .Resolve(session)
            .Routes
            .Any(route => string.Equals(
                    route.RouteId,
                    travel.RouteId,
                    StringComparison.Ordinal)
                && route.RequiredProgressIds.Contains(
                    session.Scenario.ProgressId,
                    StringComparer.Ordinal));
    }

    private enum AdvanceAvailability
    {
        Available = 0,
        WrongMode = 1,
        WrongProgress = 2,
        Complete = 3
    }
}
