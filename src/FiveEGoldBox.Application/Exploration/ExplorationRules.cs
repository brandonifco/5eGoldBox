using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

public static class ExplorationRules
{
    public static ApplicationSessionState EnterWatchtower(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.CurrentMode
            != ApplicationMode.RegionalTravel)
        {
            throw new InvalidOperationException(
                "The watchtower may be entered only after regional travel reaches it.");
        }

        RegionalTravelState travel =
            canonicalSession.RegionalTravel!;

        if (!travel.IsComplete)
        {
            throw new InvalidOperationException(
                "The watchtower may be entered only after the regional journey is complete.");
        }

        if (!string.Equals(
            travel.RouteId,
            WatchtowerRegionalRoute.RouteId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The completed regional route is unsupported for watchtower entry.");
        }

        if (!string.Equals(
            travel.DestinationLocationId,
            WatchtowerRegionalRoute.WatchtowerLocationId,
            StringComparison.Ordinal)
            || !string.Equals(
                canonicalSession.CurrentLocationId,
                WatchtowerRegionalRoute
                    .WatchtowerLocationId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The completed journey did not arrive at the ruined watchtower.");
        }

        if (canonicalSession.Scenario.Progress
            != WatchtowerScenarioProgress.MissionAccepted)
        {
            throw new InvalidOperationException(
                "The watchtower may be entered only while the accepted mission is active.");
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

    public static ApplicationSessionState UseStairs(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            RequireExplorationSession(session);
        ExplorationState exploration =
            canonicalSession.Exploration!;

        if (!WatchtowerExplorationMap
            .TryGetStairDestination(
                exploration.Floor,
                exploration.Position,
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
}
