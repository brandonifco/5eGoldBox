using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

public static class ExplorationRules
{
    public static bool CanEnterDestination(
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

        return GetDestinationEntryAvailability(
            canonicalSession)
            == DestinationEntryAvailability.Available;
    }

    public static ApplicationSessionState EnterDestination(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        switch (GetDestinationEntryAvailability(
            canonicalSession))
        {
            case DestinationEntryAvailability.Available:
                break;
            case DestinationEntryAvailability.WrongMode:
                throw new InvalidOperationException(
                    "The destination may be entered only after regional travel reaches it.");
            case DestinationEntryAvailability.IncompleteJourney:
                throw new InvalidOperationException(
                    "The destination may be entered only after the regional journey is complete.");
            case DestinationEntryAvailability.UnsupportedRoute:
                throw new InvalidOperationException(
                    "The completed regional route is unsupported for destination entry.");
            case DestinationEntryAvailability.WrongDestination:
                throw new InvalidOperationException(
                    "The completed journey did not arrive at its declared destination.");
            case DestinationEntryAvailability.WrongProgress:
                throw new InvalidOperationException(
                    "The destination may be entered only while its route remains open.");
            default:
                throw new InvalidOperationException(
                    "The destination-entry availability could not be resolved.");
        }

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                CurrentMode = ApplicationMode.Exploration,
                RegionalTravel = null,
                Exploration = ScenarioExplorationMap.CreateStartingState(
                    RequireMap(canonicalSession))
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
        ExplorationMapDefinition map =
            ScenarioExplorationMap.FindCurrent(canonicalSession)
            ?? throw new InvalidOperationException(
                "The exploration location has no map.");
        bool didMove =
            ScenarioExplorationMap.CanMoveBetween(
                map,
                exploration.Floor,
                exploration.Position,
                destination,
                exploration.OpenDoorIds);

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
            out string destinationFloor,
            out GridPosition destinationPosition))
        {
            throw new InvalidOperationException(
                "The party is not standing on an authored staircase.");
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

    public static bool CanOpenDoor(
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

        return ResolveOpenableDoor(canonicalSession) is not null;
    }

    public static ApplicationSessionState OpenDoor(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            RequireExplorationSession(session);
        ExplorationState exploration =
            canonicalSession.Exploration!;

        DoorDefinition door =
            ResolveOpenableDoor(canonicalSession)
            ?? throw new InvalidOperationException(
                "There is no openable door ahead of the party.");

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                Exploration = exploration with
                {
                    OpenDoorIds = exploration.OpenDoorIds
                        .Append(door.DoorId)
                        .ToArray()
                }
            });
    }

    public static bool CanRevealSecretDoor(
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

        return ResolveRevealableSecretDoor(canonicalSession) is not null;
    }

    public static ApplicationSessionState RevealSecretDoor(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            RequireExplorationSession(session);
        ExplorationState exploration =
            canonicalSession.Exploration!;

        DoorDefinition door =
            ResolveRevealableSecretDoor(canonicalSession)
            ?? throw new InvalidOperationException(
                "There is no undiscovered secret door ahead of the party.");

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                Exploration = exploration with
                {
                    RevealedSecretDoorIds = exploration
                        .RevealedSecretDoorIds
                        .Append(door.DoorId)
                        .ToArray()
                }
            });
    }

    public static bool CanCollectTreasure(
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

        return ResolveCollectableTreasure(canonicalSession) is not null;
    }

    public static ApplicationSessionState CollectTreasure(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            RequireExplorationSession(session);
        ExplorationState exploration =
            canonicalSession.Exploration!;

        TreasureDefinition treasure =
            ResolveCollectableTreasure(canonicalSession)
            ?? throw new InvalidOperationException(
                "There is no uncollected treasure where the party is standing.");

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                Exploration = exploration with
                {
                    CollectedTreasureIds = exploration
                        .CollectedTreasureIds
                        .Append(treasure.TreasureId)
                        .ToArray()
                },
                Party = GrantTreasure(canonicalSession.Party, treasure)
            });
    }

    /// Adds a treasure's reward to the party's shared purse -- gold pieces
    /// added directly, and, if the treasure names an item, either an
    /// existing stack of that item incremented or a new one appended.
    private static PartyState GrantTreasure(
        PartyState party,
        TreasureDefinition treasure)
    {
        CurrencyAmount currency = party.Currency;

        if (treasure.GoldPieces is int goldPieces
            && goldPieces != 0)
        {
            currency = currency with
            {
                GoldPieces = currency.GoldPieces + goldPieces
            };
        }

        IReadOnlyList<PartyInventoryItemState> inventoryItems =
            party.InventoryItems;

        if (treasure.ItemId is string itemId)
        {
            inventoryItems = PartyInventoryRules.AddItem(
                inventoryItems,
                itemId,
                treasure.Quantity ?? 1);
        }

        return party with
        {
            Currency = currency,
            InventoryItems = inventoryItems
        };
    }

    public static bool CanTalkToNpc(
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

        return FindNpcToTalkTo(canonicalSession) is not null;
    }

    /// Talking to an NPC has no session-state consequence -- it is a pure
    /// read, unlike opening a door or collecting treasure -- so this
    /// returns the line itself rather than an updated
    /// ApplicationSessionState.
    public static string DescribeNpcDialogue(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            RequireExplorationSession(session);

        NpcDefinition npc =
            FindNpcToTalkTo(canonicalSession)
            ?? throw new InvalidOperationException(
                "There is no one to talk to ahead of the party.");

        return $"{npc.Name}: {npc.DialogueText}";
    }

    /// The NPC ahead of the party, if one is there -- an NPC blocks its own
    /// tile like a door, so this checks the same forward position rather
    /// than the party's own square the way treasure does. Internal rather
    /// than private so SessionView can name the NPC in a command's display
    /// text without ExplorationRules exposing NpcDefinition itself.
    internal static NpcDefinition? FindNpcToTalkTo(
        ApplicationSessionState session)
    {
        ExplorationState exploration =
            session.Exploration!;
        GridPosition forward =
            GetForwardPosition(
                exploration.Position,
                exploration.Facing);

        return ScenarioExplorationMap.FindNpc(
            RequireMap(session),
            exploration.Floor,
            forward);
    }

    /// A read-only projection of the current floor's grid geometry plus
    /// the party's real position/facing, for a client to draw a real area
    /// map from — the same "public Query wrapping otherwise-internal
    /// definitions" shape CombatOperations.Query establishes for combat.
    /// Null outside exploration, or at a location with no explorable
    /// floor (a hub).
    public static ExplorationMapView? Query(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.CurrentMode != ApplicationMode.Exploration
            || canonicalSession.Exploration is not ExplorationState exploration)
        {
            return null;
        }

        ExplorationMapDefinition? map =
            ScenarioExplorationMap.FindCurrent(canonicalSession);

        return map is null
            ? null
            : ExplorationMapViewFactory.Create(map, exploration);
    }

    private static DestinationEntryAvailability
        GetDestinationEntryAvailability(
            ApplicationSessionState session)
    {
        if (session.CurrentMode
            != ApplicationMode.RegionalTravel)
        {
            return DestinationEntryAvailability.WrongMode;
        }

        RegionalTravelState travel =
            session.RegionalTravel!;

        if (!travel.IsComplete)
        {
            return DestinationEntryAvailability
                .IncompleteJourney;
        }

        TravelRouteDefinition? route = ScenarioDefinitionRegistry
            .Resolve(session)
            .Routes
            .FirstOrDefault(candidate => string.Equals(
                candidate.RouteId,
                travel.RouteId,
                StringComparison.Ordinal));

        if (route is null)
        {
            return DestinationEntryAvailability
                .UnsupportedRoute;
        }

        // The party must have arrived at, and be standing in, the place the
        // route leads to.
        if (!string.Equals(
                travel.DestinationLocationId,
                route.DestinationLocationId,
                StringComparison.Ordinal)
            || !string.Equals(
                session.CurrentLocationId,
                route.DestinationLocationId,
                StringComparison.Ordinal))
        {
            return DestinationEntryAvailability
                .WrongDestination;
        }

        if (!route.RequiredProgressIds.Contains(
            session.Scenario.ProgressId,
            StringComparer.Ordinal))
        {
            return DestinationEntryAvailability.WrongProgress;
        }

        return DestinationEntryAvailability.Available;
    }

    private static bool TryGetStairDestination(
        ApplicationSessionState session,
        out string destinationFloor,
        out GridPosition destinationPosition)
    {
        ExplorationState exploration =
            session.Exploration!;

        return ScenarioExplorationMap.TryGetStairDestination(
            RequireMap(session),
            exploration.Floor,
            exploration.Position,
            out destinationFloor,
            out destinationPosition);
    }

    /// The door ahead of the party, if one exists there, is not locked, and
    /// (being either not secret or already found) is legal to open right
    /// now -- whether or not it has already been opened.
    private static DoorDefinition? ResolveOpenableDoor(
        ApplicationSessionState session)
    {
        ExplorationState exploration =
            session.Exploration!;
        GridPosition forward =
            GetForwardPosition(
                exploration.Position,
                exploration.Facing);
        DoorDefinition? door = ScenarioExplorationMap.FindDoorBetween(
            RequireMap(session),
            exploration.Floor,
            exploration.Position,
            forward);

        if (door is null
            || door.IsLocked
            || (door.IsSecret
                && !exploration.RevealedSecretDoorIds.Contains(
                    door.DoorId,
                    StringComparer.Ordinal))
            || exploration.OpenDoorIds.Contains(
                door.DoorId,
                StringComparer.Ordinal))
        {
            return null;
        }

        return door;
    }

    /// The secret door ahead of the party, if one exists there and has not
    /// already been found. Locked-ness does not gate revealing -- a locked
    /// secret door can still be discovered, it just can never be opened.
    private static DoorDefinition? ResolveRevealableSecretDoor(
        ApplicationSessionState session)
    {
        ExplorationState exploration =
            session.Exploration!;
        GridPosition forward =
            GetForwardPosition(
                exploration.Position,
                exploration.Facing);
        DoorDefinition? door = ScenarioExplorationMap.FindDoorBetween(
            RequireMap(session),
            exploration.Floor,
            exploration.Position,
            forward);

        if (door is null
            || !door.IsSecret
            || exploration.RevealedSecretDoorIds.Contains(
                door.DoorId,
                StringComparer.Ordinal))
        {
            return null;
        }

        return door;
    }

    /// The treasure the party is standing on, if any, that has not already
    /// been collected. Unlike a door, this checks the party's own current
    /// position rather than the position ahead -- treasure sits on an
    /// already-walkable tile the party can stand on.
    private static TreasureDefinition? ResolveCollectableTreasure(
        ApplicationSessionState session)
    {
        ExplorationState exploration =
            session.Exploration!;
        TreasureDefinition? treasure = ScenarioExplorationMap.FindTreasure(
            RequireMap(session),
            exploration.Floor,
            exploration.Position);

        if (treasure is null
            || exploration.CollectedTreasureIds.Contains(
                treasure.TreasureId,
                StringComparer.Ordinal))
        {
            return null;
        }

        return treasure;
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
        return ExplorationFacingOffsets.Apply(facing, position);
    }

    private enum DestinationEntryAvailability
    {
        Available = 0,
        WrongMode = 1,
        IncompleteJourney = 2,
        UnsupportedRoute = 3,
        WrongDestination = 4,
        WrongProgress = 5
    }

    private static ExplorationMapDefinition RequireMap(
        ApplicationSessionState session)
    {
        return ScenarioExplorationMap.FindCurrent(session)
            ?? throw new InvalidOperationException(
                "The exploration location has no map.");
    }
}
