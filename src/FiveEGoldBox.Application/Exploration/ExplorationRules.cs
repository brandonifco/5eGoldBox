using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

public static class ExplorationRules
{
    public static bool CanEnterWatchtower(
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

        return GetWatchtowerEntryAvailability(
            canonicalSession)
            == WatchtowerEntryAvailability.Available;
    }

    public static ApplicationSessionState EnterWatchtower(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        switch (GetWatchtowerEntryAvailability(
            canonicalSession))
        {
            case WatchtowerEntryAvailability.Available:
                break;
            case WatchtowerEntryAvailability.WrongMode:
                throw new InvalidOperationException(
                    "The watchtower may be entered only after regional travel reaches it.");
            case WatchtowerEntryAvailability.IncompleteJourney:
                throw new InvalidOperationException(
                    "The watchtower may be entered only after the regional journey is complete.");
            case WatchtowerEntryAvailability.UnsupportedRoute:
                throw new InvalidOperationException(
                    "The completed regional route is unsupported for watchtower entry.");
            case WatchtowerEntryAvailability.WrongDestination:
                throw new InvalidOperationException(
                    "The completed journey did not arrive at the ruined watchtower.");
            case WatchtowerEntryAvailability.WrongProgress:
                throw new InvalidOperationException(
                    "The watchtower may be entered only while the accepted mission is active.");
            default:
                throw new InvalidOperationException(
                    "The watchtower-entry availability could not be resolved.");
        }

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                CurrentMode = ApplicationMode.Exploration,
                RegionalTravel = null,
                Exploration = WatchtowerExplorationMap
                    .CreateStartingState()
            });
    }

    public static ApplicationSessionState Turn(
        ApplicationSessionState session,
        ExplorationTurnDirection direction)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Unsupported exploration turn direction.");
        }

        ApplicationSessionState canonicalSession =
            RequireExplorationSession(session);
        ExplorationState exploration =
            canonicalSession.Exploration!;

        ExplorationFacing facing = direction switch
        {
            ExplorationTurnDirection.Left =>
                TurnLeft(exploration.Facing),
            ExplorationTurnDirection.Right =>
                TurnRight(exploration.Facing),
            _ => throw new InvalidOperationException(
                "The validated exploration turn direction could not be resolved.")
        };

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                Exploration = exploration with
                {
                    Facing = facing
                }
            });
    }

    public static ExplorationMoveResult MoveForward(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            RequireExplorationSession(session);
        ExplorationState exploration =
            canonicalSession.Exploration!;
        GridPosition destination =
            GetForwardPosition(
                exploration.Position,
                exploration.Facing);
        bool didMove =
            WatchtowerExplorationMap.IsTraversable(
                exploration.Floor,
                destination);

        ApplicationSessionState resultingSession =
            didMove
                ? ApplicationSessionRules.CreateCanonical(
                    canonicalSession with
                    {
                        Exploration = exploration with
                        {
                            Position = destination
                        }
                    })
                : canonicalSession;

        return new ExplorationMoveResult
        {
            DidMove = didMove,
            State = resultingSession
        };
    }

    public static bool CanUseStairs(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CurrentMode
            != ApplicationMode.Exploration)
        {
            return false;
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        return TryGetStairDestination(
            canonicalSession,
            out _,
            out _);
    }

    public static ApplicationSessionState UseStairs(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            RequireExplorationSession(session);
        ExplorationState exploration =
            canonicalSession.Exploration!;

        if (!TryGetStairDestination(
            canonicalSession,
            out ExplorationFloor destinationFloor,
            out GridPosition destinationPosition))
        {
            throw new InvalidOperationException(
                "The party is not standing on the authored watchtower staircase.");
        }

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                Exploration = exploration with
                {
                    Floor = destinationFloor,
                    Position = destinationPosition
                }
            });
    }

    private static WatchtowerEntryAvailability
        GetWatchtowerEntryAvailability(
            ApplicationSessionState session)
    {
        if (session.CurrentMode
            != ApplicationMode.RegionalTravel)
        {
            return WatchtowerEntryAvailability.WrongMode;
        }

        RegionalTravelState travel =
            session.RegionalTravel!;

        if (!travel.IsComplete)
        {
            return WatchtowerEntryAvailability
                .IncompleteJourney;
        }

        if (!string.Equals(
            travel.RouteId,
            WatchtowerRegionalRoute.RouteId,
            StringComparison.Ordinal))
        {
            return WatchtowerEntryAvailability
                .UnsupportedRoute;
        }

        if (!string.Equals(
                travel.DestinationLocationId,
                WatchtowerRegionalRoute
                    .WatchtowerLocationId,
                StringComparison.Ordinal)
            || !string.Equals(
                session.CurrentLocationId,
                WatchtowerRegionalRoute
                    .WatchtowerLocationId,
                StringComparison.Ordinal))
        {
            return WatchtowerEntryAvailability
                .WrongDestination;
        }

        if (session.Scenario.Progress
            != WatchtowerScenarioProgress.MissionAccepted)
        {
            return WatchtowerEntryAvailability.WrongProgress;
        }

        return WatchtowerEntryAvailability.Available;
    }

    private static bool TryGetStairDestination(
        ApplicationSessionState session,
        out ExplorationFloor destinationFloor,
        out GridPosition destinationPosition)
    {
        ExplorationState exploration =
            session.Exploration!;

        return WatchtowerExplorationMap
            .TryGetStairDestination(
                exploration.Floor,
                exploration.Position,
                out destinationFloor,
                out destinationPosition);
    }

    private static ApplicationSessionState
        RequireExplorationSession(
            ApplicationSessionState session)
    {
        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.CurrentMode
            != ApplicationMode.Exploration)
        {
            throw new InvalidOperationException(
                "The requested exploration action is available only in exploration mode.");
        }

        return canonicalSession;
    }

    private static ExplorationFacing TurnLeft(
        ExplorationFacing facing)
    {
        return facing switch
        {
            ExplorationFacing.North =>
                ExplorationFacing.West,
            ExplorationFacing.West =>
                ExplorationFacing.South,
            ExplorationFacing.South =>
                ExplorationFacing.East,
            ExplorationFacing.East =>
                ExplorationFacing.North,
            _ => throw new InvalidOperationException(
                "The validated exploration facing could not turn left.")
        };
    }

    private static ExplorationFacing TurnRight(
        ExplorationFacing facing)
    {
        return facing switch
        {
            ExplorationFacing.North =>
                ExplorationFacing.East,
            ExplorationFacing.East =>
                ExplorationFacing.South,
            ExplorationFacing.South =>
                ExplorationFacing.West,
            ExplorationFacing.West =>
                ExplorationFacing.North,
            _ => throw new InvalidOperationException(
                "The validated exploration facing could not turn right.")
        };
    }

    private static GridPosition GetForwardPosition(
        GridPosition position,
        ExplorationFacing facing)
    {
        return facing switch
        {
            ExplorationFacing.North =>
                position with
                {
                    Y = position.Y - 1
                },
            ExplorationFacing.East =>
                position with
                {
                    X = position.X + 1
                },
            ExplorationFacing.South =>
                position with
                {
                    Y = position.Y + 1
                },
            ExplorationFacing.West =>
                position with
                {
                    X = position.X - 1
                },
            _ => throw new InvalidOperationException(
                "The validated exploration facing could not produce forward movement.")
        };
    }

    private enum WatchtowerEntryAvailability
    {
        Available = 0,
        WrongMode = 1,
        IncompleteJourney = 2,
        UnsupportedRoute = 3,
        WrongDestination = 4,
        WrongProgress = 5
    }
}
