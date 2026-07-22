using System.Globalization;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Console.Tests;

public sealed class ConsoleProcessRestartTests
{
    private const int DefaultRandomSeed = 1;

    [Fact]
    public async Task RestartedPreSignalSave_ProducesSameFinalVictorySaveAsUninterruptedProcess()
    {
        using TemporaryDirectory controlDirectory = new();
        using TemporaryDirectory restartedDirectory = new();
        VictoryProcessPlan plan = CreateVictoryProcessPlan();

        ConsoleProcessResult control = await RunProcessAsync(
            controlDirectory,
            plan.UninterruptedSelections);
        ConsoleProcessResult firstRestartProcess = await RunProcessAsync(
            restartedDirectory,
            plan.PreSignalSaveSelections);
        ConsoleProcessResult secondRestartProcess = await RunProcessAsync(
            restartedDirectory,
            plan.RestartedContinuationSelections);

        Assert.NotEqual(
            firstRestartProcess.ProcessId,
            secondRestartProcess.ProcessId);
        Assert.True(firstRestartProcess.ExitedBeforeReturn);
        Assert.True(secondRestartProcess.ExitedBeforeReturn);

        byte[] controlBytes = File.ReadAllBytes(
            controlDirectory.SavePath);
        byte[] restartedBytes = File.ReadAllBytes(
            restartedDirectory.SavePath);

        Assert.True(
            controlBytes.SequenceEqual(restartedBytes),
            control.CreateFailureDiagnostic(
                controlDirectory.SavePath)
            + secondRestartProcess.CreateFailureDiagnostic(
                restartedDirectory.SavePath));

        ApplicationSessionState controlSession = DeserializeSave(
            controlDirectory.SavePath);
        ApplicationSessionState restartedSession = DeserializeSave(
            restartedDirectory.SavePath);

        AssertEquivalentFinalState(
            plan.ExpectedFinalState,
            controlSession);
        AssertEquivalentFinalState(
            plan.ExpectedFinalState,
            restartedSession);
        AssertEquivalentFinalState(
            controlSession,
            restartedSession);
        Assert.Contains(
            "Outcome: PartyVictory",
            control.StandardOutput);
        Assert.Contains(
            "Outcome: PartyVictory",
            secondRestartProcess.StandardOutput);
    }

    [Fact]
    public async Task RestartedPreSignalSave_PreservesRandomCursorThroughFutureCombat()
    {
        using TemporaryDirectory controlDirectory = new();
        using TemporaryDirectory restartedDirectory = new();
        VictoryProcessPlan plan = CreateVictoryProcessPlan();

        _ = await RunProcessAsync(
            controlDirectory,
            plan.UninterruptedSelections);
        _ = await RunProcessAsync(
            restartedDirectory,
            plan.PreSignalSaveSelections);

        ApplicationSessionState savedBeforeRestart = DeserializeSave(
            restartedDirectory.SavePath);

        _ = await RunProcessAsync(
            restartedDirectory,
            plan.RestartedContinuationSelections);

        ApplicationSessionState controlFinal = DeserializeSave(
            controlDirectory.SavePath);
        ApplicationSessionState restartedFinal = DeserializeSave(
            restartedDirectory.SavePath);

        Assert.Equal(
            plan.PreSignalState.RandomSeed,
            savedBeforeRestart.RandomSeed);
        Assert.Equal(
            plan.PreSignalState.RandomValuesConsumed,
            savedBeforeRestart.RandomValuesConsumed);
        Assert.Equal(
            savedBeforeRestart.RandomSeed,
            restartedFinal.RandomSeed);
        Assert.True(
            restartedFinal.RandomValuesConsumed
                > savedBeforeRestart.RandomValuesConsumed);
        Assert.Equal(
            controlFinal.RandomSeed,
            restartedFinal.RandomSeed);
        Assert.Equal(
            controlFinal.RandomValuesConsumed,
            restartedFinal.RandomValuesConsumed);
        Assert.Equal(
            plan.ExpectedFinalState.RandomValuesConsumed,
            restartedFinal.RandomValuesConsumed);
    }

    [Fact]
    public async Task RestartedVictorySave_LoadsAndContinuesExploration()
    {
        using TemporaryDirectory temporary = new();
        VictorySaveContinuationPlan plan =
            CreateVictorySaveContinuationPlan();

        ConsoleProcessResult victoryProcess = await RunProcessAsync(
            temporary,
            plan.CreateVictorySaveSelections);
        ApplicationSessionState beforeRestart = DeserializeSave(
            temporary.SavePath);
        byte[] projectedPartyBeforeRestart = SerializePartyProjection(
            beforeRestart);

        ConsoleProcessResult continuationProcess = await RunProcessAsync(
            temporary,
            plan.LoadTurnSaveSelections);
        ApplicationSessionState afterRestart = DeserializeSave(
            temporary.SavePath);

        Assert.NotEqual(
            victoryProcess.ProcessId,
            continuationProcess.ProcessId);
        Assert.True(victoryProcess.ExitedBeforeReturn);
        Assert.True(continuationProcess.ExitedBeforeReturn);
        AssertEquivalentFinalState(
            plan.ExpectedFinalizedState,
            beforeRestart);
        Assert.Equal(
            ApplicationMode.Exploration,
            beforeRestart.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.RaidersDefeated,
            beforeRestart.Scenario.Progress);
        Assert.Equal(
            plan.ExpectedLoadedAndTurnedState.Exploration,
            afterRestart.Exploration);
        Assert.NotEqual(
            beforeRestart.Exploration!.Facing,
            afterRestart.Exploration!.Facing);
        Assert.Equal(
            projectedPartyBeforeRestart,
            SerializePartyProjection(afterRestart));
        Assert.Equal(
            plan.ExpectedLoadedAndTurnedState.RandomSeed,
            afterRestart.RandomSeed);
        Assert.Equal(
            plan.ExpectedLoadedAndTurnedState.RandomValuesConsumed,
            afterRestart.RandomValuesConsumed);
        Assert.Equal(
            WatchtowerScenarioProgress.RaidersDefeated,
            afterRestart.Scenario.Progress);
        Assert.Equal(
            beforeRestart.CurrentLocationId,
            afterRestart.CurrentLocationId);
        Assert.Null(afterRestart.ActiveEncounter);
        Assert.Contains(
            "Turned right.",
            continuationProcess.StandardOutput);
    }

    [Fact]
    public async Task RestartedScenarioDefeatSave_PreservesTerminalStateAndPartyProjection()
    {
        using TemporaryDirectory temporary = new();
        DefeatProcessPlan plan = CreateDefeatProcessPlan();

        ConsoleProcessResult firstProcess = await RunProcessAsync(
            temporary,
            plan.PreSignalSaveSelections);
        ConsoleProcessResult defeatProcess = await RunProcessAsync(
            temporary,
            plan.RestartedDefeatSelections);
        ApplicationSessionState savedDefeat = DeserializeSave(
            temporary.SavePath);
        ConsoleProcessResult reloadProcess = await RunProcessAsync(
            temporary,
            plan.ReloadInspectSaveSelections);
        ApplicationSessionState reloadedDefeat = DeserializeSave(
            temporary.SavePath);

        Assert.NotEqual(
            firstProcess.ProcessId,
            defeatProcess.ProcessId);
        Assert.NotEqual(
            defeatProcess.ProcessId,
            reloadProcess.ProcessId);
        AssertEquivalentFinalState(
            plan.ExpectedDefeatState,
            savedDefeat);
        AssertEquivalentFinalState(
            plan.ExpectedDefeatState,
            reloadedDefeat);
        Assert.Equal(
            ApplicationMode.ScenarioConclusion,
            reloadedDefeat.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.PartyDefeated,
            reloadedDefeat.Scenario.Progress);
        Assert.Null(reloadedDefeat.Exploration);
        Assert.Null(reloadedDefeat.RegionalTravel);
        Assert.Null(reloadedDefeat.ActiveEncounter);
        Assert.Contains(
            "Outcome: ScenarioDefeat",
            defeatProcess.StandardOutput);
        Assert.Contains(
            "Party Inspection",
            reloadProcess.StandardOutput);

        string terminalMenu = GetLastMenu(
            reloadProcess.StandardOutput,
            "Session Menu");

        Assert.Equal(
            string.Join(
                '\n',
                "Session Menu",
                "1. Inspect Party",
                "2. Save",
                "3. Exit",
                "Selection: "),
            terminalMenu);
    }

    [Fact]
    public async Task RestartedScenarioDefeatSave_ReserializesByteIdentically()
    {
        using TemporaryDirectory temporary = new();
        DefeatProcessPlan plan = CreateDefeatProcessPlan();

        _ = await RunProcessAsync(
            temporary,
            plan.PreSignalSaveSelections);
        _ = await RunProcessAsync(
            temporary,
            plan.RestartedDefeatSelections);

        byte[] firstDefeatSave = File.ReadAllBytes(
            temporary.SavePath);
        ApplicationSessionState firstDefeatState = DeserializeSave(
            temporary.SavePath);

        _ = await RunProcessAsync(
            temporary,
            plan.ReloadInspectSaveSelections);

        byte[] secondDefeatSave = File.ReadAllBytes(
            temporary.SavePath);
        ApplicationSessionState secondDefeatState = DeserializeSave(
            temporary.SavePath);

        Assert.True(
            firstDefeatSave.SequenceEqual(secondDefeatSave),
            $"The defeat save changed after load and resave at '{temporary.SavePath}'.");
        AssertEquivalentFinalState(
            firstDefeatState,
            secondDefeatState);
        Assert.Equal(
            ApplicationMode.ScenarioConclusion,
            secondDefeatState.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.PartyDefeated,
            secondDefeatState.Scenario.Progress);
    }

    [Fact]
    public async Task EncounterProcessExit_DoesNotCreateOrOverwriteSave()
    {
        using TemporaryDirectory noExistingSaveDirectory = new();
        using TemporaryDirectory existingSaveDirectory = new();
        EncounterExitPlan plan = CreateEncounterExitPlan();

        ConsoleProcessResult noSaveProcess = await RunProcessAsync(
            noExistingSaveDirectory,
            plan.NewGameEncounterExitSelections);

        Assert.False(File.Exists(
            noExistingSaveDirectory.SavePath));
        Assert.DoesNotContain(
            "Game saved",
            noSaveProcess.StandardOutput);
        AssertEncounterMenuHasNoSave(
            noSaveProcess.StandardOutput);

        _ = await RunProcessAsync(
            existingSaveDirectory,
            plan.PreSignalSaveSelections);
        byte[] originalSave = File.ReadAllBytes(
            existingSaveDirectory.SavePath);

        ConsoleProcessResult loadedEncounterProcess = await RunProcessAsync(
            existingSaveDirectory,
            plan.LoadEncounterExitSelections);
        byte[] retainedSave = File.ReadAllBytes(
            existingSaveDirectory.SavePath);

        Assert.True(
            originalSave.SequenceEqual(retainedSave),
            loadedEncounterProcess.CreateFailureDiagnostic(
                existingSaveDirectory.SavePath));
        Assert.DoesNotContain(
            "Game saved",
            loadedEncounterProcess.StandardOutput);
        AssertEncounterMenuHasNoSave(
            loadedEncounterProcess.StandardOutput);
    }

    [Fact]
    public async Task SeparateRestartRuns_UseDistinctExitedProcesses()
    {
        using TemporaryDirectory temporary = new();

        string[] exitSelections = ["3"];
        ConsoleProcessResult first = await RunProcessAsync(
            temporary,
            exitSelections);
        ConsoleProcessResult second = await RunProcessAsync(
            temporary,
            exitSelections);

        Assert.True(
            first.ProcessId != second.ProcessId,
            first.CreateFailureDiagnostic(temporary.SavePath)
            + second.CreateFailureDiagnostic(temporary.SavePath));
        Assert.True(first.ExitedBeforeReturn);
        Assert.True(second.ExitedBeforeReturn);
        Assert.False(first.TimedOut);
        Assert.False(second.TimedOut);
        Assert.Equal<int?>(0, first.ExitCode);
        Assert.Equal<int?>(0, second.ExitCode);
    }

    private static VictoryProcessPlan CreateVictoryProcessPlan()
    {
        SignalReadyPlan signalReady = CreateSignalReadyPlan();
        ApplicationSessionState encounter =
            SignalMechanismRules.Activate(signalReady.State);
        CombatPlan combat = CreateCombatPlan(
            encounter,
            CombatStrategy.PartyVictory);
        ApplicationSessionState finalized = combat.Outcome.State;
        ApplicationSessionState continued = ExplorationRules.Turn(
            finalized,
            ExplorationTurnDirection.Right);

        List<string> uninterrupted =
            new(signalReady.NewGameSelections);
        uninterrupted.Add(GetSessionSelection(
            signalReady.State,
            SessionAction.ActivateSignal));
        uninterrupted.AddRange(combat.Selections);
        uninterrupted.Add(GetSessionSelection(
            finalized,
            SessionAction.TurnRight));
        uninterrupted.Add(GetSessionSelection(
            continued,
            SessionAction.Save));
        uninterrupted.Add(GetSessionSelection(
            continued,
            SessionAction.Exit));

        List<string> preSignalSave =
            new(signalReady.NewGameSelections);
        preSignalSave.Add(GetSessionSelection(
            signalReady.State,
            SessionAction.Save));
        preSignalSave.Add(GetSessionSelection(
            signalReady.State,
            SessionAction.Exit));

        List<string> restartedContinuation =
        [
            "2",
            GetSessionSelection(
                signalReady.State,
                SessionAction.ActivateSignal)
        ];
        restartedContinuation.AddRange(combat.Selections);
        restartedContinuation.Add(GetSessionSelection(
            finalized,
            SessionAction.TurnRight));
        restartedContinuation.Add(GetSessionSelection(
            continued,
            SessionAction.Save));
        restartedContinuation.Add(GetSessionSelection(
            continued,
            SessionAction.Exit));

        return new VictoryProcessPlan(
            PreSignalState: signalReady.State,
            ExpectedFinalState: continued,
            UninterruptedSelections: uninterrupted.AsReadOnly(),
            PreSignalSaveSelections: preSignalSave.AsReadOnly(),
            RestartedContinuationSelections:
                restartedContinuation.AsReadOnly());
    }

    private static VictorySaveContinuationPlan
        CreateVictorySaveContinuationPlan()
    {
        SignalReadyPlan signalReady = CreateSignalReadyPlan();
        ApplicationSessionState encounter =
            SignalMechanismRules.Activate(signalReady.State);
        CombatPlan combat = CreateCombatPlan(
            encounter,
            CombatStrategy.PartyVictory);
        ApplicationSessionState finalized = combat.Outcome.State;
        ApplicationSessionState turned = ExplorationRules.Turn(
            finalized,
            ExplorationTurnDirection.Right);

        List<string> createVictorySave =
            new(signalReady.NewGameSelections);
        createVictorySave.Add(GetSessionSelection(
            signalReady.State,
            SessionAction.ActivateSignal));
        createVictorySave.AddRange(combat.Selections);
        createVictorySave.Add(GetSessionSelection(
            finalized,
            SessionAction.Save));
        createVictorySave.Add(GetSessionSelection(
            finalized,
            SessionAction.Exit));

        string[] loadTurnSave =
        [
            "2",
            GetSessionSelection(
                finalized,
                SessionAction.TurnRight),
            GetSessionSelection(
                turned,
                SessionAction.Save),
            GetSessionSelection(
                turned,
                SessionAction.Exit)
        ];

        return new VictorySaveContinuationPlan(
            ExpectedFinalizedState: finalized,
            ExpectedLoadedAndTurnedState: turned,
            CreateVictorySaveSelections:
                createVictorySave.AsReadOnly(),
            LoadTurnSaveSelections: loadTurnSave);
    }

    private static DefeatProcessPlan CreateDefeatProcessPlan()
    {
        SignalReadyPlan signalReady = CreateSignalReadyPlan();
        ApplicationSessionState encounter =
            SignalMechanismRules.Activate(signalReady.State);
        CombatPlan combat = CreateCombatPlan(
            encounter,
            CombatStrategy.ScenarioDefeat);
        ApplicationSessionState defeat = combat.Outcome.State;

        List<string> preSignalSave =
            new(signalReady.NewGameSelections);
        preSignalSave.Add(GetSessionSelection(
            signalReady.State,
            SessionAction.Save));
        preSignalSave.Add(GetSessionSelection(
            signalReady.State,
            SessionAction.Exit));

        List<string> restartedDefeat =
        [
            "2",
            GetSessionSelection(
                signalReady.State,
                SessionAction.ActivateSignal)
        ];
        restartedDefeat.AddRange(combat.Selections);
        restartedDefeat.Add(GetSessionSelection(
            defeat,
            SessionAction.Save));
        restartedDefeat.Add(GetSessionSelection(
            defeat,
            SessionAction.Exit));

        string[] reloadInspectSave =
        [
            "2",
            GetSessionSelection(
                defeat,
                SessionAction.InspectParty),
            GetSessionSelection(
                defeat,
                SessionAction.Save),
            GetSessionSelection(
                defeat,
                SessionAction.Exit)
        ];

        return new DefeatProcessPlan(
            ExpectedDefeatState: defeat,
            PreSignalSaveSelections: preSignalSave.AsReadOnly(),
            RestartedDefeatSelections:
                restartedDefeat.AsReadOnly(),
            ReloadInspectSaveSelections: reloadInspectSave);
    }

    private static EncounterExitPlan CreateEncounterExitPlan()
    {
        SignalReadyPlan signalReady = CreateSignalReadyPlan();
        ApplicationSessionState encounter =
            SignalMechanismRules.Activate(signalReady.State);
        WatchtowerCombatResolutionResult normalized =
            WatchtowerCombatRules.AdvanceToDecision(encounter);
        int exitSelection = GetCombatExitSelection(
            normalized.ResultingDecision);

        List<string> newGameEncounterExit =
            new(signalReady.NewGameSelections);
        newGameEncounterExit.Add(GetSessionSelection(
            signalReady.State,
            SessionAction.ActivateSignal));
        newGameEncounterExit.Add(ToSelection(exitSelection));

        List<string> preSignalSave =
            new(signalReady.NewGameSelections);
        preSignalSave.Add(GetSessionSelection(
            signalReady.State,
            SessionAction.Save));
        preSignalSave.Add(GetSessionSelection(
            signalReady.State,
            SessionAction.Exit));

        string[] loadEncounterExit =
        [
            "2",
            GetSessionSelection(
                signalReady.State,
                SessionAction.ActivateSignal),
            ToSelection(exitSelection)
        ];

        return new EncounterExitPlan(
            NewGameEncounterExitSelections:
                newGameEncounterExit.AsReadOnly(),
            PreSignalSaveSelections: preSignalSave.AsReadOnly(),
            LoadEncounterExitSelections: loadEncounterExit);
    }

    private static SignalReadyPlan CreateSignalReadyPlan()
    {
        List<string> selections = ["1"];
        ApplicationSessionState state =
            WatchtowerScenarioSessionFactory.CreateNew(
                DefaultRandomSeed);

        state = ApplySessionAction(
            selections,
            state,
            SessionAction.AcceptMission);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.BeginWatchtowerJourney);

        while (RegionalTravelRules.CanAdvance(state))
        {
            state = ApplySessionAction(
                selections,
                state,
                SessionAction.AdvanceTravel);
        }

        state = ApplySessionAction(
            selections,
            state,
            SessionAction.EnterWatchtower);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.MoveForward);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.MoveForward);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.UseStairs);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.TurnRight);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.MoveForward);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.TurnRight);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.MoveForward);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.TurnLeft);
        state = ApplySessionAction(
            selections,
            state,
            SessionAction.TurnLeft);

        if (!SignalMechanismRules.CanActivate(state))
        {
            throw new InvalidOperationException(
                "The public noncombat script did not reach the signal mechanism.");
        }

        return new SignalReadyPlan(
            selections.AsReadOnly(),
            state);
    }

    private static ApplicationSessionState ApplySessionAction(
        ICollection<string> selections,
        ApplicationSessionState state,
        SessionAction action)
    {
        selections.Add(GetSessionSelection(state, action));

        return action switch
        {
            SessionAction.AcceptMission =>
                OutpostMissionRules.Resolve(
                    state,
                    OutpostMissionChoice.AcceptMission).State,
            SessionAction.BeginWatchtowerJourney =>
                RegionalTravelRules.BeginWatchtowerJourney(state),
            SessionAction.AdvanceTravel =>
                RegionalTravelRules.Advance(state).State,
            SessionAction.EnterWatchtower =>
                ExplorationRules.EnterWatchtower(state),
            SessionAction.MoveForward =>
                ExplorationRules.MoveForward(state).State,
            SessionAction.TurnLeft =>
                ExplorationRules.Turn(
                    state,
                    ExplorationTurnDirection.Left),
            SessionAction.TurnRight =>
                ExplorationRules.Turn(
                    state,
                    ExplorationTurnDirection.Right),
            SessionAction.UseStairs =>
                ExplorationRules.UseStairs(state),
            _ => throw new InvalidOperationException(
                $"The scripted session action '{action}' is not a state transition.")
        };
    }

    private static string GetSessionSelection(
        ApplicationSessionState state,
        SessionAction action)
    {
        IReadOnlyList<SessionAction> actions =
            CreateSessionActions(state);
        int index = actions.ToList().IndexOf(action);

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"The session action '{action}' is not available in mode '{state.CurrentMode}' with progress '{state.Scenario.Progress}'.");
        }

        return ToSelection(index + 1);
    }

    private static IReadOnlyList<SessionAction>
        CreateSessionActions(
            ApplicationSessionState state)
    {
        List<SessionAction> actions = new();

        switch (state.CurrentMode)
        {
            case ApplicationMode.Outpost:
                foreach (OutpostMissionChoice choice
                    in OutpostMissionRules.GetAvailableChoices(state))
                {
                    actions.Add(
                        choice switch
                        {
                            OutpostMissionChoice.AcceptMission =>
                                SessionAction.AcceptMission,
                            OutpostMissionChoice.NotYet =>
                                SessionAction.DeferMission,
                            _ => throw new InvalidOperationException(
                                "The public outpost choice is unsupported by the process-test script builder.")
                        });
                }

                if (RegionalTravelRules
                    .CanBeginWatchtowerJourney(state))
                {
                    actions.Add(
                        SessionAction.BeginWatchtowerJourney);
                }

                break;
            case ApplicationMode.RegionalTravel:
                if (RegionalTravelRules.CanAdvance(state))
                {
                    actions.Add(SessionAction.AdvanceTravel);
                }

                if (ExplorationRules.CanEnterWatchtower(state))
                {
                    actions.Add(SessionAction.EnterWatchtower);
                }

                break;
            case ApplicationMode.Exploration:
                actions.Add(SessionAction.MoveForward);
                actions.Add(SessionAction.TurnLeft);
                actions.Add(SessionAction.TurnRight);

                if (ExplorationRules.CanUseStairs(state))
                {
                    actions.Add(SessionAction.UseStairs);
                }

                if (SignalMechanismRules.CanActivate(state))
                {
                    actions.Add(SessionAction.ActivateSignal);
                }

                break;
            case ApplicationMode.ScenarioConclusion:
                break;
            case ApplicationMode.Encounter:
                throw new InvalidOperationException(
                    "Encounter selections must be derived from the public combat decision.");
            default:
                throw new InvalidOperationException(
                    $"The application mode '{state.CurrentMode}' is unsupported by the process-test script builder.");
        }

        actions.Add(SessionAction.InspectParty);

        if (ManualSaveSerializer.CanSerialize(state))
        {
            actions.Add(SessionAction.Save);
        }

        actions.Add(SessionAction.Exit);
        return actions.AsReadOnly();
    }

    private static CombatPlan CreateCombatPlan(
        ApplicationSessionState encounter,
        CombatStrategy strategy)
    {
        List<string> selections = new();
        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(encounter);
        ApplicationSessionState state = result.State;
        WatchtowerCombatDecision decision =
            result.ResultingDecision;

        for (int operation = 0; operation < 1000; operation++)
        {
            if (decision.State
                == WatchtowerCombatDecisionState.CombatCompleted)
            {
                WatchtowerCombatOutcomeResult outcome =
                    WatchtowerCombatOutcomeRules.Finalize(state);

                return new CombatPlan(
                    Selections: selections.AsReadOnly(),
                    Outcome: outcome);
            }

            if (decision.State
                != WatchtowerCombatDecisionState
                    .PlayerDecisionRequired)
            {
                throw new InvalidOperationException(
                    $"The public combat seam returned unexpected decision state '{decision.State}'.");
            }

            IReadOnlyList<WatchtowerCombatMovementDestinationOption>
                movements = GetAvailableMovements(decision);
            IReadOnlyList<WatchtowerCombatTargetOption> targets =
                GetAvailableTargets(decision);

            if (strategy == CombatStrategy.PartyVictory
                && targets.Count > 0)
            {
                WatchtowerCombatTargetOption target = targets
                    .OrderBy(candidate =>
                        GetParticipantCurrentHitPoints(
                            state,
                            candidate.TargetCombatantId))
                    .ThenBy(candidate =>
                        candidate.TargetCombatantId,
                        StringComparer.Ordinal)
                    .First();
                int targetIndex = targets.ToList()
                    .IndexOf(target);
                int selection = movements.Count
                    + targetIndex
                    + 1;
                selections.Add(ToSelection(selection));
                result = WatchtowerCombatRules.Execute(
                    state,
                    new WatchtowerCombatWeaponAttackIntent
                    {
                        ExpectedEncounterRevision =
                            decision.EncounterRevision,
                        ActorCombatantId =
                            decision.ActiveCombatantId
                            ?? throw new InvalidOperationException(
                                "A player combat decision did not identify the active combatant."),
                        WeaponId = decision.WeaponAttack!.WeaponId,
                        TargetCombatantId =
                            target.TargetCombatantId
                    });
            }
            else if (strategy == CombatStrategy.PartyVictory
                && movements.Count > 0)
            {
                WatchtowerCombatMovementDestinationOption movement =
                    SelectMovementTowardNearestTarget(
                        state,
                        decision,
                        movements);
                int selection = movements.ToList()
                    .IndexOf(movement)
                    + 1;
                selections.Add(ToSelection(selection));
                result = WatchtowerCombatRules.Execute(
                    state,
                    new WatchtowerCombatMoveIntent
                    {
                        ExpectedEncounterRevision =
                            decision.EncounterRevision,
                        ActorCombatantId =
                            decision.ActiveCombatantId
                            ?? throw new InvalidOperationException(
                                "A player combat decision did not identify the active combatant."),
                        Path = movement.Path
                    });
            }
            else
            {
                if (decision.EndTurn?.IsAvailable != true)
                {
                    throw new InvalidOperationException(
                        "The deterministic defeat strategy required an unavailable End Turn operation.");
                }

                int selection = movements.Count
                    + targets.Count
                    + 1;
                selections.Add(ToSelection(selection));
                result = WatchtowerCombatRules.Execute(
                    state,
                    new WatchtowerCombatEndTurnIntent
                    {
                        ExpectedEncounterRevision =
                            decision.EncounterRevision,
                        ActorCombatantId =
                            decision.ActiveCombatantId
                            ?? throw new InvalidOperationException(
                                "A player combat decision did not identify the active combatant.")
                    });
            }

            state = result.State;
            decision = result.ResultingDecision;
        }

        throw new InvalidOperationException(
            "The deterministic public combat strategy did not complete within 1000 player operations.");
    }

    private static WatchtowerCombatMovementDestinationOption
        SelectMovementTowardNearestTarget(
            ApplicationSessionState state,
            WatchtowerCombatDecision decision,
            IReadOnlyList<WatchtowerCombatMovementDestinationOption>
                movements)
    {
        WatchtowerCombatTargetOption target =
            decision.WeaponAttack!.Targets
                .OrderBy(candidate =>
                    candidate.DistanceFeet ?? int.MaxValue)
                .ThenBy(candidate =>
                    candidate.TargetCombatantId,
                    StringComparer.Ordinal)
                .First();
        var targetParticipant = state.ActiveEncounter!.Encounter
            .Participants.Single(participant => string.Equals(
                participant.Combatant.CombatantId,
                target.TargetCombatantId,
                StringComparison.Ordinal));

        return movements
            .OrderBy(movement =>
                Math.Abs(
                    movement.Destination.X
                    - targetParticipant.Position.X)
                + Math.Abs(
                    movement.Destination.Y
                    - targetParticipant.Position.Y))
            .ThenByDescending(movement =>
                movement.MovementSpentFeet)
            .ThenBy(movement => movement.Destination.X)
            .ThenBy(movement => movement.Destination.Y)
            .First();
    }

    private static IReadOnlyList<WatchtowerCombatMovementDestinationOption>
        GetAvailableMovements(
            WatchtowerCombatDecision decision)
    {
        return decision.Movement?.IsAvailable == true
            ? decision.Movement.DestinationOptions
            : Array.Empty<WatchtowerCombatMovementDestinationOption>();
    }

    private static IReadOnlyList<WatchtowerCombatTargetOption>
        GetAvailableTargets(
            WatchtowerCombatDecision decision)
    {
        return decision.WeaponAttack?.IsAvailable == true
            ? decision.WeaponAttack.Targets
                .Where(target => target.IsAvailable)
                .ToArray()
            : Array.Empty<WatchtowerCombatTargetOption>();
    }

    private static int GetCombatExitSelection(
        WatchtowerCombatDecision decision)
    {
        if (decision.State
            != WatchtowerCombatDecisionState.PlayerDecisionRequired)
        {
            throw new InvalidOperationException(
                "A combat Exit selection requires a public player decision.");
        }

        return GetAvailableMovements(decision).Count
            + GetAvailableTargets(decision).Count
            + (decision.EndTurn?.IsAvailable == true ? 1 : 0)
            + 2;
    }

    private static int GetParticipantCurrentHitPoints(
        ApplicationSessionState state,
        string combatantId)
    {
        return state.ActiveEncounter!.Encounter.Participants
            .Single(participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal))
            .Combatant.Health.HitPoints.CurrentHitPoints;
    }

    private static async Task<ConsoleProcessResult> RunProcessAsync(
        TemporaryDirectory temporary,
        IReadOnlyList<string> selections)
    {
        ConsoleProcessResult result =
            await ConsoleProcessHarness.RunAsync(
                temporary.DirectoryPath,
                CreateScriptedInput(selections));

        Assert.True(
            !result.TimedOut
            && result.ExitedBeforeReturn
            && result.ExitCode == 0,
            result.CreateFailureDiagnostic(
                temporary.SavePath));

        return result;
    }

    private static string CreateScriptedInput(
        IReadOnlyList<string> selections)
    {
        return string.Join(
            Environment.NewLine,
            selections.Append(string.Empty));
    }

    private static ApplicationSessionState DeserializeSave(
        string savePath)
    {
        string serialized = File.ReadAllText(savePath);
        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(serialized);

        Assert.True(
            result.IsSuccess,
            $"The process-created save failed to deserialize: {result.FailureReason}");
        return Assert.IsType<ApplicationSessionState>(
            result.Session);
    }

    private static void AssertEquivalentFinalState(
        ApplicationSessionState expected,
        ApplicationSessionState actual)
    {
        Assert.Equal(expected.ScenarioId, actual.ScenarioId);
        Assert.Equal(expected.CurrentMode, actual.CurrentMode);
        Assert.Equal(
            expected.CurrentLocationId,
            actual.CurrentLocationId);
        Assert.Equal(
            expected.Scenario.Progress,
            actual.Scenario.Progress);
        Assert.Equal(expected.RandomSeed, actual.RandomSeed);
        Assert.Equal(
            expected.RandomValuesConsumed,
            actual.RandomValuesConsumed);
        Assert.Equal(
            expected.Party.PartyId,
            actual.Party.PartyId);
        Assert.Equal(
            expected.Party.Members.Count,
            actual.Party.Members.Count);

        for (int index = 0;
            index < expected.Party.Members.Count;
            index++)
        {
            var expectedMember = expected.Party.Members[index];
            var actualMember = actual.Party.Members[index];

            Assert.Equal(
                expectedMember.PartyMemberId,
                actualMember.PartyMemberId);
            Assert.Equal(
                expectedMember.CharacterDefinitionId,
                actualMember.CharacterDefinitionId);
            Assert.Equal(
                expectedMember.DisplayName,
                actualMember.DisplayName);
            Assert.Equal(
                expectedMember.ClassId,
                actualMember.ClassId);
            Assert.Equal(
                expectedMember.ZeroHitPointPolicy,
                actualMember.ZeroHitPointPolicy);
            Assert.Equal(
                expectedMember.Health.HitPoints.MaximumHitPoints,
                actualMember.Health.HitPoints.MaximumHitPoints);
            Assert.Equal(
                expectedMember.Health.HitPoints.CurrentHitPoints,
                actualMember.Health.HitPoints.CurrentHitPoints);
            Assert.Equal(
                expectedMember.Health.HitPoints.TemporaryHitPoints,
                actualMember.Health.HitPoints.TemporaryHitPoints);
            Assert.Equal(
                expectedMember.Health.DeathSavingThrows.SuccessCount,
                actualMember.Health.DeathSavingThrows.SuccessCount);
            Assert.Equal(
                expectedMember.Health.DeathSavingThrows.FailureCount,
                actualMember.Health.DeathSavingThrows.FailureCount);
            Assert.Equal(
                expectedMember.Health.DeathSavingThrows.IsStable,
                actualMember.Health.DeathSavingThrows.IsStable);
            Assert.Equal(
                expectedMember.Health.IsInstantlyDead,
                actualMember.Health.IsInstantlyDead);
            Assert.Equal(
                expectedMember.Health.IsDead,
                actualMember.Health.IsDead);

            if (expectedMember.Ammunition is null)
            {
                Assert.Null(actualMember.Ammunition);
            }
            else
            {
                Assert.NotNull(actualMember.Ammunition);
                Assert.Equal(
                    expectedMember.Ammunition.WeaponId,
                    actualMember.Ammunition.WeaponId);
                Assert.Equal(
                    expectedMember.Ammunition.AmmunitionItemId,
                    actualMember.Ammunition.AmmunitionItemId);
                Assert.Equal(
                    expectedMember.Ammunition.RemainingQuantity,
                    actualMember.Ammunition.RemainingQuantity);
            }
        }

        if (expected.Exploration is null)
        {
            Assert.Null(actual.Exploration);
        }
        else
        {
            Assert.NotNull(actual.Exploration);
            Assert.Equal(
                expected.Exploration.MapId,
                actual.Exploration.MapId);
            Assert.Equal(
                expected.Exploration.Floor,
                actual.Exploration.Floor);
            Assert.Equal(
                expected.Exploration.Position.X,
                actual.Exploration.Position.X);
            Assert.Equal(
                expected.Exploration.Position.Y,
                actual.Exploration.Position.Y);
            Assert.Equal(
                expected.Exploration.Facing,
                actual.Exploration.Facing);
        }

        Assert.Null(actual.RegionalTravel);
        Assert.Null(actual.ActiveEncounter);
    }

    private static byte[] SerializePartyProjection(
        ApplicationSessionState state)
    {
        List<string> values =
        [
            state.Party.PartyId
        ];

        foreach (var member in state.Party.Members)
        {
            values.Add(member.PartyMemberId);
            values.Add(member.CharacterDefinitionId);
            values.Add(member.DisplayName);
            values.Add(member.ClassId);
            values.Add(member.ZeroHitPointPolicy.ToString());
            values.Add(member.Health.HitPoints.MaximumHitPoints
                .ToString(CultureInfo.InvariantCulture));
            values.Add(member.Health.HitPoints.CurrentHitPoints
                .ToString(CultureInfo.InvariantCulture));
            values.Add(member.Health.HitPoints.TemporaryHitPoints
                .ToString(CultureInfo.InvariantCulture));
            values.Add(member.Health.DeathSavingThrows.SuccessCount
                .ToString(CultureInfo.InvariantCulture));
            values.Add(member.Health.DeathSavingThrows.FailureCount
                .ToString(CultureInfo.InvariantCulture));
            values.Add(member.Health.DeathSavingThrows.IsStable
                .ToString());
            values.Add(member.Health.IsInstantlyDead
                .ToString());
            values.Add(member.Health.IsDead
                .ToString());
            values.Add(member.Ammunition?.WeaponId ?? string.Empty);
            values.Add(
                member.Ammunition?.AmmunitionItemId
                ?? string.Empty);
            values.Add(
                member.Ammunition?.RemainingQuantity.ToString(
                    CultureInfo.InvariantCulture)
                ?? string.Empty);
        }

        return System.Text.Encoding.UTF8.GetBytes(
            string.Join('|', values));
    }

    private static void AssertEncounterMenuHasNoSave(
        string output)
    {
        string combatMenu = GetLastMenu(
            output,
            "Combat Menu");

        Assert.Contains(
            "Inspect Encounter",
            combatMenu);
        Assert.Contains(
            "Exit",
            combatMenu);
        Assert.DoesNotContain(
            "Save",
            combatMenu);
    }

    private static string GetLastMenu(
        string output,
        string heading)
    {
        int start = output.LastIndexOf(
            heading);

        Assert.True(
            start >= 0,
            $"Expected menu heading '{heading}'.{Environment.NewLine}{output}");

        int end = output.IndexOf(
            "Selection: ",
            start);

        Assert.True(
            end >= 0,
            $"Expected selection prompt after '{heading}'.{Environment.NewLine}{output}");

        return output[
            start..(end + "Selection: ".Length)];
    }

    private static string ToSelection(int selection)
    {
        return selection.ToString(
            CultureInfo.InvariantCulture);
    }

    private sealed record SignalReadyPlan(
        IReadOnlyList<string> NewGameSelections,
        ApplicationSessionState State);

    private sealed record CombatPlan(
        IReadOnlyList<string> Selections,
        WatchtowerCombatOutcomeResult Outcome);

    private sealed record VictoryProcessPlan(
        ApplicationSessionState PreSignalState,
        ApplicationSessionState ExpectedFinalState,
        IReadOnlyList<string> UninterruptedSelections,
        IReadOnlyList<string> PreSignalSaveSelections,
        IReadOnlyList<string> RestartedContinuationSelections);

    private sealed record VictorySaveContinuationPlan(
        ApplicationSessionState ExpectedFinalizedState,
        ApplicationSessionState ExpectedLoadedAndTurnedState,
        IReadOnlyList<string> CreateVictorySaveSelections,
        IReadOnlyList<string> LoadTurnSaveSelections);

    private sealed record DefeatProcessPlan(
        ApplicationSessionState ExpectedDefeatState,
        IReadOnlyList<string> PreSignalSaveSelections,
        IReadOnlyList<string> RestartedDefeatSelections,
        IReadOnlyList<string> ReloadInspectSaveSelections);

    private sealed record EncounterExitPlan(
        IReadOnlyList<string> NewGameEncounterExitSelections,
        IReadOnlyList<string> PreSignalSaveSelections,
        IReadOnlyList<string> LoadEncounterExitSelections);

    private enum CombatStrategy
    {
        PartyVictory,
        ScenarioDefeat
    }

    private enum SessionAction
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

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"FiveEGoldBox.Console.ProcessTests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            SavePath = Path.Combine(
                DirectoryPath,
                "savegame.json");
        }

        internal string DirectoryPath { get; }

        internal string SavePath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(
                    DirectoryPath,
                    recursive: true);
            }
        }
    }
}
