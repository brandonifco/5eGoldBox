using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Console.Tests;

public sealed class ConsoleSessionRunnerNoncombatTests
{
    private const int RandomSeed = 13579;

    [Fact]
    public void RunSession_OutpostMissionChoicesUseAuthoritativeOrder()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateOutpostSession());

        string menu = GetLastSessionMenu(output);
        AssertMenuOrder(
            menu,
            "1. Accept Mission",
            "2. Not Yet",
            "3. Inspect Party",
            "4. Save",
            "5. Exit");
    }

    [Fact]
    public void RunSession_AcceptMissionAdoptsReturnedState()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateOutpostSession();
        ApplicationSessionState expected =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission)
            .State;

        string output = RunSession(
            "1\n",
            temporary.SavePath,
            session);

        Assert.Contains("Mission accepted.", output);
        Assert.Contains(
            $"Progress: {expected.Scenario.Progress}",
            output);
        Assert.Contains("1. Begin Watchtower Journey", output);
    }

    [Fact]
    public void RunSession_NotYetAdoptsUnchangedAuthoritativeState()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateOutpostSession();
        var party = session.Party;
        var scenario = session.Scenario;
        int randomValuesConsumed =
            session.RandomValuesConsumed;

        string output = RunSession(
            "2\n",
            temporary.SavePath,
            session);

        Assert.Contains("Mission deferred.", output);
        Assert.Equal(
            2,
            CountOccurrences(
                output,
                "Progress: MissionNotAccepted"));
        Assert.Same(party, session.Party);
        Assert.Same(scenario, session.Scenario);
        Assert.Equal(
            randomValuesConsumed,
            session.RandomValuesConsumed);
    }

    [Fact]
    public void RunSession_MissionAcceptedMenuShowsJourneyBeforeCommonOptions()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateMissionAcceptedSession());

        AssertMenuOrder(
            GetLastSessionMenu(output),
            "1. Begin Watchtower Journey",
            "2. Inspect Party",
            "3. Save",
            "4. Exit");
        Assert.DoesNotContain("Accept Mission", output);
        Assert.DoesNotContain("Not Yet", output);
    }

    [Fact]
    public void RunSession_BeginJourneyAdoptsRegionalTravelState()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateMissionAcceptedSession();
        ApplicationSessionState expected =
            RegionalTravelRules.BeginWatchtowerJourney(
                session);

        string output = RunSession(
            "1\n",
            temporary.SavePath,
            session);

        Assert.Contains("Watchtower journey begun.", output);
        Assert.Contains("Mode: RegionalTravel", output);
        Assert.Contains(
            $"Route ID: {expected.RegionalTravel!.RouteId}",
            output);
        Assert.Contains("1. Advance Travel", output);
    }


    [Fact]
    public void RunSession_OutpostTravelAndEntrySubmissionsDoNotMutateInputs()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState outpost =
            CreateOutpostSession();
        ApplicationSessionState accepted =
            CreateMissionAcceptedSession();
        ApplicationSessionState travel =
            CreateRegionalTravelSession();
        ApplicationSessionState completedTravel =
            CreateCompletedTravelSession();
        var outpostParty = outpost.Party;
        var outpostScenario = outpost.Scenario;
        var acceptedScenario = accepted.Scenario;
        var travelState = travel.RegionalTravel;
        var completedTravelState =
            completedTravel.RegionalTravel;

        _ = RunSession(
            "1\n",
            temporary.SavePath,
            outpost);
        _ = RunSession(
            "1\n",
            temporary.SavePath,
            accepted);
        _ = RunSession(
            "1\n",
            temporary.SavePath,
            travel);
        _ = RunSession(
            "1\n",
            temporary.SavePath,
            completedTravel);

        Assert.Same(outpostParty, outpost.Party);
        Assert.Same(outpostScenario, outpost.Scenario);
        Assert.Same(acceptedScenario, accepted.Scenario);
        Assert.Same(travelState, travel.RegionalTravel);
        Assert.Same(
            completedTravelState,
            completedTravel.RegionalTravel);
        Assert.Equal(0, outpost.RandomValuesConsumed);
        Assert.Equal(0, accepted.RandomValuesConsumed);
        Assert.Equal(0, travel.RandomValuesConsumed);
        Assert.Equal(
            0,
            completedTravel.RandomValuesConsumed);
    }

    [Fact]
    public void RunSession_IncompleteTravelMenuIsExactAndUnsaveable()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateRegionalTravelSession());

        string menu = GetLastSessionMenu(output);
        AssertMenuOrder(
            menu,
            "1. Advance Travel",
            "2. Inspect Party",
            "3. Exit");
        Assert.DoesNotContain("Save", menu);
        Assert.DoesNotContain("Enter Watchtower", menu);
    }

    [Fact]
    public void RunSession_AdvanceTravelMovesExactlyOneStep()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateRegionalTravelSession();
        RegionalTravelAdvanceResult expected =
            RegionalTravelRules.Advance(session);

        string output = RunSession(
            "1\n",
            temporary.SavePath,
            session);

        Assert.Contains("Travel advanced.", output);
        Assert.Contains(
            $"Current Step Index: {expected.State.RegionalTravel!.CurrentStepIndex}",
            output);
        Assert.DoesNotContain("Destination reached.", output);
    }

    [Fact]
    public void RunSession_FinalTravelAdvanceReportsArrival()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateTravelBeforeArrivalSession();
        RegionalTravelAdvanceResult expected =
            RegionalTravelRules.Advance(session);

        string output = RunSession(
            "1\n",
            temporary.SavePath,
            session);

        Assert.True(expected.DidArrive);
        Assert.Contains("Destination reached.", output);
        Assert.Contains("Travel Complete: Yes", output);
        Assert.Contains("1. Enter Watchtower", output);
        Assert.DoesNotContain(
            "1. Advance Travel",
            GetLastSessionMenu(output));
    }

    [Fact]
    public void RunSession_CompletedTravelMenuIsExact()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateCompletedTravelSession());

        AssertMenuOrder(
            GetLastSessionMenu(output),
            "1. Enter Watchtower",
            "2. Inspect Party",
            "3. Exit");
    }

    [Fact]
    public void RunSession_EnterWatchtowerAdoptsExplorationState()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateCompletedTravelSession();
        ApplicationSessionState expected =
            ExplorationRules.EnterWatchtower(session);

        string output = RunSession(
            "1\n",
            temporary.SavePath,
            session);

        Assert.Contains(
            $"Entered location: {expected.CurrentLocationId}.",
            output);
        Assert.Contains("Mode: Exploration", output);
        Assert.Contains(
            $"Position X: {expected.Exploration!.Position.X}",
            output);
        Assert.Contains(
            $"Position Y: {expected.Exploration.Position.Y}",
            output);
    }

    [Fact]
    public void RunSession_OrdinaryExplorationMenuIsExact()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateExplorationSession());

        AssertMenuOrder(
            GetLastSessionMenu(output),
            "1. Move Forward",
            "2. Turn Left",
            "3. Turn Right",
            "4. Inspect Party",
            "5. Save",
            "6. Exit");
    }

    [Fact]
    public void RunSession_MoveForwardAdoptsReturnedPosition()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateExplorationSession();
        ExplorationMoveResult expected =
            ExplorationRules.MoveForward(session);

        string output = RunSession(
            "1\n",
            temporary.SavePath,
            session);

        Assert.True(expected.DidMove);
        Assert.Contains("Moved forward.", output);
        Assert.Contains(
            $"Position X: {expected.State.Exploration!.Position.X}",
            output);
        Assert.Contains(
            $"Position Y: {expected.State.Exploration.Position.Y}",
            output);
    }

    [Fact]
    public void RunSession_BlockedMoveUsesReturnedUnchangedPosition()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateExplorationSession();
        ApplicationSessionState facingNorth =
            ExplorationRules.Turn(
                session,
                ExplorationTurnDirection.Left);
        ExplorationMoveResult expected =
            ExplorationRules.MoveForward(facingNorth);

        string output = RunSession(
            "2\n1\n",
            temporary.SavePath,
            session);

        Assert.False(expected.DidMove);
        Assert.Contains("Turned left.", output);
        Assert.Contains("Movement blocked.", output);
        Assert.Contains(
            $"Position X: {expected.State.Exploration!.Position.X}",
            output);
        Assert.Contains(
            $"Position Y: {expected.State.Exploration.Position.Y}",
            output);
    }

    [Fact]
    public void RunSession_TurnLeftAdoptsReturnedFacing()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateExplorationSession();
        ApplicationSessionState expected =
            ExplorationRules.Turn(
                session,
                ExplorationTurnDirection.Left);

        string output = RunSession(
            "2\n",
            temporary.SavePath,
            session);

        Assert.Contains("Turned left.", output);
        Assert.Contains(
            $"Facing: {expected.Exploration!.Facing}",
            output);
    }

    [Fact]
    public void RunSession_TurnRightAdoptsReturnedFacing()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateExplorationSession();
        ApplicationSessionState expected =
            ExplorationRules.Turn(
                session,
                ExplorationTurnDirection.Right);

        string output = RunSession(
            "3\n",
            temporary.SavePath,
            session);

        Assert.Contains("Turned right.", output);
        Assert.Contains(
            $"Facing: {expected.Exploration!.Facing}",
            output);
    }

    [Fact]
    public void RunSession_PreSignalExplorationActionsDoNotMutateInputOrRandomness()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateExplorationSession();
        var party = session.Party;
        var scenario = session.Scenario;
        var exploration = session.Exploration;
        int randomValuesConsumed =
            session.RandomValuesConsumed;

        _ = RunSession(
            "1\n2\n3\n",
            temporary.SavePath,
            session);

        Assert.Same(party, session.Party);
        Assert.Same(scenario, session.Scenario);
        Assert.Same(exploration, session.Exploration);
        Assert.Equal(
            randomValuesConsumed,
            session.RandomValuesConsumed);
    }

    [Fact]
    public void RunSession_StairsAreOmittedAwayFromStairPosition()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateExplorationSession());

        Assert.DoesNotContain(
            "Use Stairs",
            GetLastSessionMenu(output));
    }

    [Fact]
    public void RunSession_StairMenuIsExact()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateStairSession());

        AssertMenuOrder(
            GetLastSessionMenu(output),
            "1. Move Forward",
            "2. Turn Left",
            "3. Turn Right",
            "4. Use Stairs",
            "5. Inspect Party",
            "6. Save",
            "7. Exit");
        Assert.DoesNotContain("Activate Signal", output);
    }

    [Fact]
    public void RunSession_UseStairsAdoptsReturnedFloorAndPosition()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateStairSession();
        ApplicationSessionState expected =
            ExplorationRules.UseStairs(session);

        string output = RunSession(
            "4\n",
            temporary.SavePath,
            session);

        Assert.Contains("Used stairs.", output);
        Assert.Contains(
            $"Floor: {expected.Exploration!.Floor}",
            output);
        Assert.Contains(
            $"Position X: {expected.Exploration.Position.X}",
            output);
        Assert.Contains(
            $"Position Y: {expected.Exploration.Position.Y}",
            output);
    }


    [Fact]
    public void RunSession_UseStairsDoesNotMutateInputOrRandomness()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateStairSession();
        var exploration = session.Exploration;
        int randomValuesConsumed =
            session.RandomValuesConsumed;

        _ = RunSession(
            "4\n",
            temporary.SavePath,
            session);

        Assert.Same(exploration, session.Exploration);
        Assert.Equal(
            randomValuesConsumed,
            session.RandomValuesConsumed);
    }

    [Fact]
    public void RunSession_SignalIsOmittedAtWrongPositionOrFacing()
    {
        using TemporaryDirectory temporary = new();

        string ordinaryOutput = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateExplorationSession());
        string stairOutput = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateStairSession());

        Assert.DoesNotContain(
            "Activate Signal",
            GetLastSessionMenu(ordinaryOutput));
        Assert.DoesNotContain(
            "Activate Signal",
            GetLastSessionMenu(stairOutput));
    }

    [Fact]
    public void RunSession_SignalMenuIsExact()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateSignalReadySession());

        AssertMenuOrder(
            GetLastSessionMenu(output),
            "1. Move Forward",
            "2. Turn Left",
            "3. Turn Right",
            "4. Activate Signal",
            "5. Inspect Party",
            "6. Save",
            "7. Exit");
        Assert.DoesNotContain("Use Stairs", output);
    }

    [Fact]
    public void RunSession_ActivateSignalAdoptsApplicationEncounterAndRandomCursor()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateSignalReadySession();
        ApplicationSessionState expected =
            SignalMechanismRules.Activate(session);

        string output = RunSession(
            "4\n",
            temporary.SavePath,
            session);

        Assert.Contains(
            "Signal activated. Encounter started.",
            output);
        Assert.Contains("Mode: Encounter", output);
        Assert.Contains("Progress: SignalActivated", output);
        Assert.Contains(
            $"Random Values Consumed Before: {expected.RandomValuesConsumed}",
            output);
        Assert.Contains(
            $"Encounter ID: {expected.ActiveEncounter!.Encounter.EncounterId}",
            output);
    }

    [Fact]
    public void RunSession_EncounterTransfersToCombatLoop()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateEncounterSession());

        Assert.Contains("Combat Resolution", output);
        Assert.Contains("Combat Decision", output);
        Assert.Contains("Combat Menu", output);
        Assert.Contains("Inspect Encounter", output);
        Assert.DoesNotContain("Inspect Party", GetLastCombatMenu(output));
        Assert.DoesNotContain("Save", GetLastCombatMenu(output));
    }

    [Fact]
    public void RunSession_ScenarioConclusionPreservesPhase9AMenu()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateScenarioConclusionSession());

        AssertMenuOrder(
            GetLastSessionMenu(output),
            "1. Inspect Party",
            "2. Save",
            "3. Exit");
    }

    [Theory]
    [InlineData("0\n3\n")]
    [InlineData("-1\n3\n")]
    [InlineData("text\n3\n")]
    [InlineData("9\n3\n")]
    public void RunSession_InvalidTravelSelectionDoesNotAdvance(
        string input)
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateRegionalTravelSession();
        int step = session.RegionalTravel!.CurrentStepIndex;
        int randomValuesConsumed =
            session.RandomValuesConsumed;

        string output = RunSession(
            input,
            temporary.SavePath,
            session);

        Assert.Contains("Invalid selection.", output);
        Assert.Equal(
            2,
            CountOccurrences(
                output,
                $"Current Step Index: {step}"));
        Assert.Equal(
            randomValuesConsumed,
            session.RandomValuesConsumed);
    }

    [Fact]
    public void RunSession_InspectPartyFollowsGameplayOptions()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateSignalReadySession());

        AssertMenuOrder(
            GetLastSessionMenu(output),
            "4. Activate Signal",
            "5. Inspect Party",
            "6. Save",
            "7. Exit");
    }

    [Fact]
    public void Run_CompleteScriptedNoncombatPathReachesEncounter()
    {
        using TemporaryDirectory temporary = new();
        string input = string.Join(
            '\n',
            "1",
            "1",
            "1",
            "1",
            "1",
            "1",
            "1",
            "1",
            "1",
            "4",
            "3",
            "1",
            "3",
            "1",
            "2",
            "2",
            "4",
            string.Empty);

        ApplicationSessionState expectedEncounter =
            CreateEncounterSession();

        (int exitCode, string output) = Run(
            input,
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(0, exitCode);
        Assert.Contains("Progress: MissionNotAccepted", output);
        Assert.Contains("Progress: MissionAccepted", output);
        Assert.Contains("Mode: RegionalTravel", output);
        Assert.Contains("Current Step Index: 1", output);
        Assert.Contains("Current Step Index: 2", output);
        Assert.Contains("Current Step Index: 3", output);
        Assert.Contains("Floor: GroundFloor", output);
        Assert.Contains("Floor: UpperFloor", output);
        Assert.Contains("Mode: Encounter", output);
        Assert.Contains("Progress: SignalActivated", output);
        Assert.Contains(
            $"Encounter Revision: {expectedEncounter.ActiveEncounter!.Encounter.Revision}",
            output);
        Assert.Contains(
            $"Random Values Consumed Before: {expectedEncounter.RandomValuesConsumed}",
            output);
        Assert.DoesNotContain("Game saved", output);
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void Run_RepeatedEquivalentScriptedPathsProduceIdenticalOutput()
    {
        using TemporaryDirectory firstTemporary = new();
        using TemporaryDirectory secondTemporary = new();
        const string Input =
            "1\n1\n1\n1\n1\n1\n1\n1\n1\n4\n3\n1\n3\n1\n2\n2\n4\n";

        (int firstExitCode, string firstOutput) = Run(
            Input,
            firstTemporary.SavePath,
            RandomSeed);
        (int secondExitCode, string secondOutput) = Run(
            Input,
            secondTemporary.SavePath,
            RandomSeed);

        Assert.Equal(firstExitCode, secondExitCode);
        Assert.Equal(firstOutput, secondOutput);
    }

    [Theory]
    [InlineData(ApplicationMode.Outpost)]
    [InlineData(ApplicationMode.RegionalTravel)]
    [InlineData(ApplicationMode.Exploration)]
    [InlineData(ApplicationMode.Encounter)]
    [InlineData(ApplicationMode.ScenarioConclusion)]
    public void RunSession_EofAtModeMenuExitsWithoutOperation(
        ApplicationMode mode)
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateSessionForMode(mode);
        int randomValuesConsumed =
            session.RandomValuesConsumed;
        var party = session.Party;

        ConsoleSessionRunner runner = new();
        int exitCode = runner.RunSession(
            new StringReader(string.Empty),
            new StringWriter(),
            temporary.SavePath,
            session);

        Assert.Equal(0, exitCode);
        Assert.Equal(
            randomValuesConsumed,
            session.RandomValuesConsumed);
        Assert.Same(party, session.Party);
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void Run_LoadedMissionAcceptedOutpostCanBeginTravel()
    {
        using TemporaryDirectory temporary = new();
        WriteSave(
            temporary.SavePath,
            CreateMissionAcceptedSession());

        (int exitCode, string output) = Run(
            "2\n1\n3\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(0, exitCode);
        Assert.Contains("Watchtower journey begun.", output);
        Assert.Contains("Mode: RegionalTravel", output);
    }

    [Fact]
    public void Run_LoadedExplorationCanPerformLegalAction()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateExplorationSession();
        ExplorationMoveResult expected =
            ExplorationRules.MoveForward(session);
        WriteSave(temporary.SavePath, session);

        (int exitCode, string output) = Run(
            "2\n1\n6\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(0, exitCode);
        Assert.Contains("Moved forward.", output);
        Assert.Contains(
            $"Position X: {expected.State.Exploration!.Position.X}",
            output);
    }

    [Fact]
    public void RunSession_RenderingMenuAloneDoesNotSubmitGameplay()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateRegionalTravelSession();
        var travel = session.RegionalTravel;
        int randomValuesConsumed =
            session.RandomValuesConsumed;

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            session);

        Assert.Contains("1. Advance Travel", output);
        Assert.Same(travel, session.RegionalTravel);
        Assert.Equal(
            randomValuesConsumed,
            session.RandomValuesConsumed);
    }

    private static (int ExitCode, string Output) Run(
        string input,
        string savePath,
        int randomSeed)
    {
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        int exitCode = runner.Run(
            new StringReader(input),
            output,
            savePath,
            randomSeed);

        return (exitCode, output.ToString());
    }

    private static string RunSession(
        string input,
        string savePath,
        ApplicationSessionState session)
    {
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        int exitCode = runner.RunSession(
            new StringReader(input),
            output,
            savePath,
            session);

        Assert.Equal(0, exitCode);
        return output.ToString();
    }

    private static ApplicationSessionState
        CreateOutpostSession()
    {
        return WatchtowerScenarioSessionFactory.CreateNew(
            RandomSeed);
    }

    private static ApplicationSessionState
        CreateMissionAcceptedSession()
    {
        return OutpostMissionRules.Resolve(
            CreateOutpostSession(),
            OutpostMissionChoice.AcceptMission)
        .State;
    }

    private static ApplicationSessionState
        CreateRegionalTravelSession()
    {
        return RegionalTravelRules.BeginWatchtowerJourney(
            CreateMissionAcceptedSession());
    }

    private static ApplicationSessionState
        CreateTravelBeforeArrivalSession()
    {
        ApplicationSessionState current =
            CreateRegionalTravelSession();

        while (current.RegionalTravel!.CurrentStepIndex
            < current.RegionalTravel.FinalStepIndex - 1)
        {
            current = RegionalTravelRules.Advance(current)
                .State;
        }

        return current;
    }

    private static ApplicationSessionState
        CreateCompletedTravelSession()
    {
        ApplicationSessionState current =
            CreateRegionalTravelSession();

        while (RegionalTravelRules.CanAdvance(current))
        {
            current = RegionalTravelRules.Advance(current)
                .State;
        }

        return current;
    }

    private static ApplicationSessionState
        CreateExplorationSession()
    {
        return ExplorationRules.EnterWatchtower(
            CreateCompletedTravelSession());
    }

    private static ApplicationSessionState
        CreateStairSession()
    {
        ApplicationSessionState current =
            CreateExplorationSession();
        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.MoveForward(current)
            .State;
        return current;
    }

    private static ApplicationSessionState
        CreateSignalReadySession()
    {
        ApplicationSessionState current =
            ExplorationRules.UseStairs(
                CreateStairSession());
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);
        return current;
    }

    private static ApplicationSessionState
        CreateEncounterSession()
    {
        return SignalMechanismRules.Activate(
            CreateSignalReadySession());
    }

    private static ApplicationSessionState
        CreateScenarioConclusionSession()
    {
        ApplicationSessionState exploration =
            CreateExplorationSession();
        ApplicationSessionState conclusion =
            exploration with
            {
                CurrentMode =
                    ApplicationMode.ScenarioConclusion,
                Scenario = exploration.Scenario with
                {
                    Progress =
                        WatchtowerScenarioProgress
                            .PartyDefeated
                },
                Exploration = null
            };

        Assert.True(
            ManualSaveSerializer.CanSerialize(conclusion));
        return conclusion;
    }

    private static ApplicationSessionState
        CreateSessionForMode(ApplicationMode mode)
    {
        return mode switch
        {
            ApplicationMode.Outpost =>
                CreateOutpostSession(),
            ApplicationMode.RegionalTravel =>
                CreateRegionalTravelSession(),
            ApplicationMode.Exploration =>
                CreateExplorationSession(),
            ApplicationMode.Encounter =>
                CreateEncounterSession(),
            ApplicationMode.ScenarioConclusion =>
                CreateScenarioConclusionSession(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unsupported test mode.")
        };
    }

    private static void WriteSave(
        string savePath,
        ApplicationSessionState session)
    {
        File.WriteAllText(
            savePath,
            ManualSaveSerializer.Serialize(session));
    }

    private static string GetLastSessionMenu(
        string output)
    {
        int start = output.LastIndexOf(
            "Session Menu",
            StringComparison.Ordinal);

        Assert.True(start >= 0);
        return output[start..];
    }

    private static string GetLastCombatMenu(
        string output)
    {
        int start = output.LastIndexOf(
            "Combat Menu",
            StringComparison.Ordinal);

        Assert.True(start >= 0);
        return output[start..];
    }

    private static void AssertMenuOrder(
        string menu,
        params string[] labels)
    {
        int previousIndex = -1;

        foreach (string label in labels)
        {
            int index = menu.IndexOf(
                label,
                StringComparison.Ordinal);

            Assert.True(
                index > previousIndex,
                $"Expected menu label '{label}' after the prior label. Menu:{Environment.NewLine}{menu}");
            previousIndex = index;
        }
    }

    private static int CountOccurrences(
        string value,
        string search)
    {
        int count = 0;
        int index = 0;

        while ((index = value.IndexOf(
            search,
            index,
            StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"FiveEGoldBox.Console.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            SavePath = Path.Combine(
                DirectoryPath,
                "savegame.json");
        }

        internal string DirectoryPath { get; }

        internal string SavePath { get; }

        public void Dispose()
        {
            Directory.Delete(
                DirectoryPath,
                recursive: true);
        }
    }
}
