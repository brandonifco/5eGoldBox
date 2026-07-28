using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Application.Views;

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
            if (session.CurrentMode == ApplicationMode.Encounter)
            {
                CombatSessionRunResult combatResult =
                    RunCombatSession(
                        input,
                        output,
                        session);

                if (combatResult.ExitRequested)
                {
                    return 0;
                }

                session = combatResult.Session;
                continue;
            }

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
                case SessionMenuAction.ResolveOutpostDecision:
                    session = ResolveOutpostDecision(
                        output,
                        session,
                        selectedOption.DecisionOptionId
                            ?? throw new InvalidOperationException(
                                "A decision menu option did not contain an option ID."));
                    break;
                case SessionMenuAction.BeginJourney:
                    session = RegionalTravelRules
                        .BeginJourney(session, selectedOption.RouteId);
                    output.WriteLine(
                        "Journey begun.");
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
                case SessionMenuAction.EnterDestination:
                    session = ExplorationRules
                        .EnterDestination(session);
                    output.WriteLine(
                        $"Entered location: {SessionView.Describe(session).LocationDisplayName}.");
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
                case SessionMenuAction.ActivateTrigger:
                    session = ScenarioTriggerRules.Activate(session);
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

    /// The engine decides what is on offer and what to call it; this only
    /// numbers the list and adds the things the client itself owns.
    ///
    /// Nothing here knows which scenario is running, or that scenarios have
    /// towers or chapels in them. Every label below the client's own three
    /// comes from authored content.
    private static IReadOnlyList<SessionMenuOption>
        CreateSessionMenuOptions(
            ApplicationSessionState session)
    {
        List<SessionMenuOption> options = new();

        foreach (SessionAction action
            in SessionView.Describe(session).Actions)
        {
            options.Add(new SessionMenuOption(
                action.DisplayName,
                ToMenuAction(action.Kind),
                action.DecisionOptionId,
                action.RouteId));
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

    private static SessionMenuAction ToMenuAction(
        SessionActionKind kind)
    {
        return kind switch
        {
            SessionActionKind.ResolveOutpostDecision =>
                SessionMenuAction.ResolveOutpostDecision,
            SessionActionKind.BeginJourney =>
                SessionMenuAction.BeginJourney,
            SessionActionKind.AdvanceTravel =>
                SessionMenuAction.AdvanceTravel,
            SessionActionKind.EnterDestination =>
                SessionMenuAction.EnterDestination,
            SessionActionKind.MoveForward =>
                SessionMenuAction.MoveForward,
            SessionActionKind.TurnLeft =>
                SessionMenuAction.TurnLeft,
            SessionActionKind.TurnRight =>
                SessionMenuAction.TurnRight,
            SessionActionKind.UseStairs =>
                SessionMenuAction.UseStairs,
            SessionActionKind.ActivateTrigger =>
                SessionMenuAction.ActivateTrigger,
            _ => throw new InvalidOperationException(
                $"The session action '{kind}' is unsupported by the console client.")
        };
    }

    private static ApplicationSessionState ResolveOutpostDecision(
        TextWriter output,
        ApplicationSessionState session,
        string optionId)
    {
        OutpostDecisionResult result =
            OutpostDecisionRules.Resolve(session, optionId);

        if (result.OptionId != optionId)
        {
            throw new InvalidOperationException(
                "The outpost decision result did not preserve the selected option.");
        }

        output.WriteLine("Choice made.");

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
        string? DecisionOptionId = null,
        string? RouteId = null);

    private enum SessionMenuAction
    {
        ResolveOutpostDecision,
        BeginJourney,
        AdvanceTravel,
        EnterDestination,
        MoveForward,
        TurnLeft,
        TurnRight,
        UseStairs,
        ActivateTrigger,
        InspectParty,
        Save,
        Exit
    }
}
