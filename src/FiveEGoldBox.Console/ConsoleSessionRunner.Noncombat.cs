using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Console;

internal sealed partial class ConsoleSessionRunner
{
    private int RunNoncombatSession(
        TextReader input,
        TextWriter output,
        string savePath,
        ApplicationSessionState session)
    {
        while (true)
        {
            RenderSessionSummary(output, session);

            IReadOnlyList<SessionMenuOption> options =
                CreateSessionMenuOptions(session);

            WriteSessionMenu(output, options);

            string? selection = input.ReadLine();

            if (selection is null)
            {
                return 0;
            }

            int selectedIndex = ParseSelection(
                selection,
                options.Count);

            if (selectedIndex == 0)
            {
                output.WriteLine("Invalid selection.");
                continue;
            }

            SessionMenuOption selectedOption =
                options[selectedIndex - 1];

            switch (selectedOption.Action)
            {
                case SessionMenuAction.AcceptMission:
                case SessionMenuAction.DeferMission:
                    session = ResolveMissionChoice(
                        output,
                        session,
                        selectedOption.MissionChoice
                            ?? throw new InvalidOperationException(
                                "A mission menu option did not contain a mission choice."));
                    break;
                case SessionMenuAction.BeginWatchtowerJourney:
                    session = RegionalTravelRules
                        .BeginWatchtowerJourney(session);
                    output.WriteLine(
                        "Watchtower journey begun.");
                    break;
                case SessionMenuAction.AdvanceTravel:
                    RegionalTravelAdvanceResult travelResult =
                        RegionalTravelRules.Advance(session);
                    session = travelResult.State;
                    output.WriteLine(
                        travelResult.DidArrive
                            ? "Destination reached."
                            : "Travel advanced.");
                    break;
                case SessionMenuAction.EnterWatchtower:
                    session = ExplorationRules
                        .EnterWatchtower(session);
                    output.WriteLine(
                        $"Entered location: {session.CurrentLocationId}.");
                    break;
                case SessionMenuAction.MoveForward:
                    ExplorationMoveResult moveResult =
                        ExplorationRules.MoveForward(session);
                    session = moveResult.State;
                    output.WriteLine(
                        moveResult.DidMove
                            ? "Moved forward."
                            : "Movement blocked.");
                    break;
                case SessionMenuAction.TurnLeft:
                    session = ExplorationRules.Turn(
                        session,
                        ExplorationTurnDirection.Left);
                    output.WriteLine("Turned left.");
                    break;
                case SessionMenuAction.TurnRight:
                    session = ExplorationRules.Turn(
                        session,
                        ExplorationTurnDirection.Right);
                    output.WriteLine("Turned right.");
                    break;
                case SessionMenuAction.UseStairs:
                    session = ExplorationRules.UseStairs(session);
                    output.WriteLine("Used stairs.");
                    break;
                case SessionMenuAction.ActivateSignal:
                    session = SignalMechanismRules.Activate(session);
                    output.WriteLine(
                        "Signal activated. Encounter started.");
                    break;
                case SessionMenuAction.InspectParty:
                    RenderParty(output, session);
                    break;
                case SessionMenuAction.Save:
                    SaveSession(output, savePath, session);
                    break;
                case SessionMenuAction.Exit:
                    return 0;
                default:
                    throw new InvalidOperationException(
                        "The selected console operation is unsupported.");
            }
        }
    }

    private static IReadOnlyList<SessionMenuOption>
        CreateSessionMenuOptions(
            ApplicationSessionState session)
    {
        List<SessionMenuOption> options = new();

        switch (session.CurrentMode)
        {
            case ApplicationMode.Outpost:
                AddOutpostOptions(options, session);
                break;
            case ApplicationMode.RegionalTravel:
                AddRegionalTravelOptions(options, session);
                break;
            case ApplicationMode.Exploration:
                AddExplorationOptions(options, session);
                break;
            case ApplicationMode.Encounter:
            case ApplicationMode.ScenarioConclusion:
                break;
            default:
                throw new InvalidOperationException(
                    "The application mode is unsupported by the console client.");
        }

        options.Add(
            new SessionMenuOption(
                "Inspect Party",
                SessionMenuAction.InspectParty));

        if (ManualSaveSerializer.CanSerialize(session))
        {
            options.Add(
                new SessionMenuOption(
                    "Save",
                    SessionMenuAction.Save));
        }

        options.Add(
            new SessionMenuOption(
                "Exit",
                SessionMenuAction.Exit));

        return options.AsReadOnly();
    }

    private static void AddOutpostOptions(
        ICollection<SessionMenuOption> options,
        ApplicationSessionState session)
    {
        IReadOnlyList<OutpostMissionChoice> choices =
            OutpostMissionRules.GetAvailableChoices(session);

        foreach (OutpostMissionChoice choice in choices)
        {
            options.Add(
                choice switch
                {
                    OutpostMissionChoice.AcceptMission =>
                        new SessionMenuOption(
                            "Accept Mission",
                            SessionMenuAction.AcceptMission,
                            choice),
                    OutpostMissionChoice.NotYet =>
                        new SessionMenuOption(
                            "Not Yet",
                            SessionMenuAction.DeferMission,
                            choice),
                    _ => throw new InvalidOperationException(
                        "The outpost mission choice is unsupported by the console client.")
                });
        }

        if (RegionalTravelRules
            .CanBeginWatchtowerJourney(session))
        {
            options.Add(
                new SessionMenuOption(
                    "Begin Watchtower Journey",
                    SessionMenuAction.BeginWatchtowerJourney));
        }
    }

    private static void AddRegionalTravelOptions(
        ICollection<SessionMenuOption> options,
        ApplicationSessionState session)
    {
        if (RegionalTravelRules.CanAdvance(session))
        {
            options.Add(
                new SessionMenuOption(
                    "Advance Travel",
                    SessionMenuAction.AdvanceTravel));
        }

        if (ExplorationRules.CanEnterWatchtower(session))
        {
            options.Add(
                new SessionMenuOption(
                    "Enter Watchtower",
                    SessionMenuAction.EnterWatchtower));
        }
    }

    private static void AddExplorationOptions(
        ICollection<SessionMenuOption> options,
        ApplicationSessionState session)
    {
        options.Add(
            new SessionMenuOption(
                "Move Forward",
                SessionMenuAction.MoveForward));
        options.Add(
            new SessionMenuOption(
                "Turn Left",
                SessionMenuAction.TurnLeft));
        options.Add(
            new SessionMenuOption(
                "Turn Right",
                SessionMenuAction.TurnRight));

        if (ExplorationRules.CanUseStairs(session))
        {
            options.Add(
                new SessionMenuOption(
                    "Use Stairs",
                    SessionMenuAction.UseStairs));
        }

        if (SignalMechanismRules.CanActivate(session))
        {
            options.Add(
                new SessionMenuOption(
                    "Activate Signal",
                    SessionMenuAction.ActivateSignal));
        }
    }

    private static ApplicationSessionState ResolveMissionChoice(
        TextWriter output,
        ApplicationSessionState session,
        OutpostMissionChoice choice)
    {
        OutpostMissionResult result =
            OutpostMissionRules.Resolve(session, choice);

        if (result.Choice != choice)
        {
            throw new InvalidOperationException(
                "The outpost mission result did not preserve the selected choice.");
        }

        switch (choice)
        {
            case OutpostMissionChoice.AcceptMission:
                if (!result.DidProgressChange)
                {
                    throw new InvalidOperationException(
                        "Accepting the mission did not report a progress change.");
                }

                output.WriteLine("Mission accepted.");
                break;
            case OutpostMissionChoice.NotYet:
                if (result.DidProgressChange)
                {
                    throw new InvalidOperationException(
                        "Deferring the mission unexpectedly reported a progress change.");
                }

                output.WriteLine("Mission deferred.");
                break;
            default:
                throw new InvalidOperationException(
                    "The selected outpost mission choice is unsupported.");
        }

        return result.State;
    }

    private static void WriteSessionMenu(
        TextWriter output,
        IReadOnlyList<SessionMenuOption> options)
    {
        output.WriteLine();
        output.WriteLine("Session Menu");

        for (int index = 0; index < options.Count; index++)
        {
            output.WriteLine(
                $"{index + 1}. {options[index].Label}");
        }

        output.Write("Selection: ");
    }

    private sealed record SessionMenuOption(
        string Label,
        SessionMenuAction Action,
        OutpostMissionChoice? MissionChoice = null);

    private enum SessionMenuAction
    {
        AcceptMission,
        DeferMission,
        BeginWatchtowerJourney,
        AdvanceTravel,
        EnterWatchtower,
        MoveForward,
        TurnLeft,
        TurnRight,
        UseStairs,
        ActivateSignal,
        InspectParty,
        Save,
        Exit
    }
}
