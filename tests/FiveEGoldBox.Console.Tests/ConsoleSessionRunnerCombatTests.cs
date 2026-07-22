using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Console.Tests;

public sealed class ConsoleSessionRunnerCombatTests
{
    private const int RandomSeed = 8675309;

    [Fact]
    public void RunSession_EncounterNormalizesAndDisplaysReturnedDecision()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        string originalEncounter = CaptureEncounterState(encounter);
        WatchtowerCombatResolutionResult expected =
            WatchtowerCombatRules.AdvanceToDecision(encounter);

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            encounter);

        Assert.Contains("Combat Resolution", output);
        Assert.Contains(
            $"Prior Encounter Revision: {expected.PriorEncounterRevision}",
            output);
        Assert.Contains(
            $"Resulting Encounter Revision: {expected.ResultingEncounterRevision}",
            output);
        Assert.Contains("Combat Decision", output);
        Assert.Contains(
            $"Encounter Revision: {expected.ResultingDecision.EncounterRevision}",
            output);
        Assert.Contains(
            $"Active Combatant ID: {expected.ResultingDecision.ActiveCombatantId}",
            output);
        Assert.Equal(originalEncounter, CaptureEncounterState(encounter));
    }

    [Fact]
    public void RunSession_CombatMenuPreservesReturnedOptionOrder()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(encounter)
                .ResultingDecision;

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            encounter);
        string menu = GetLastCombatMenu(output);
        IReadOnlyList<string> expectedLabels =
            CreateCombatLabels(decision);

        AssertMenuOrder(menu, expectedLabels);
        Assert.DoesNotContain("Save", menu);
    }

    [Fact]
    public void RenderCombatDecision_DisplaysAuthoritativeAvailability()
    {
        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(
                CreateEncounterSession())
            .ResultingDecision;
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        runner.RenderCombatDecision(output, decision);

        string text = output.ToString();
        Assert.Contains(
            $"Decision State: {decision.State}",
            text);
        Assert.Contains(
            $"Movement Remaining Feet: {decision.Movement!.MovementRemainingFeet}",
            text);
        Assert.Contains(
            $"Movement Unavailability Reason: {decision.Movement.UnavailabilityReason}",
            text);
        Assert.Contains(
            $"Weapon ID: {decision.WeaponAttack!.WeaponId}",
            text);
        Assert.Contains(
            $"Weapon Attack Unavailability Reason: {decision.WeaponAttack.UnavailabilityReason}",
            text);
        Assert.Contains(
            $"End Turn Unavailability Reason: {decision.EndTurn!.UnavailabilityReason}",
            text);
    }

    [Fact]
    public void RenderCombatDecision_AutomaticProcessingRequiredIsNotExposedAsMenu()
    {
        WatchtowerCombatDecision decision = new()
        {
            State = WatchtowerCombatDecisionState.AutomaticProcessingRequired,
            EncounterRevision = 7
        };
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => runner.RenderCombatDecision(output, decision));

        Assert.Contains(
            "Automatic combat processing remained",
            exception.Message);
        Assert.DoesNotContain("Combat Menu", output.ToString());
    }

    [Fact]
    public void RunSession_SelectedMovementUsesReturnedPathAndRendersResult()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        WatchtowerCombatResolutionResult normalized =
            WatchtowerCombatRules.AdvanceToDecision(encounter);
        WatchtowerCombatMovementDestinationOption destination =
            normalized.ResultingDecision.Movement!
                .DestinationOptions[0];
        string originalEncounter = CaptureEncounterState(encounter);
        WatchtowerCombatResolutionResult expected =
            WatchtowerCombatRules.Execute(
                normalized.State,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision =
                        normalized.ResultingDecision.EncounterRevision,
                    ActorCombatantId =
                        normalized.ResultingDecision.ActiveCombatantId!,
                    Path = destination.Path
                });

        string output = RunSession(
            "1\n",
            temporary.SavePath,
            encounter);

        Assert.Contains("Submitted Intent Kind: Move", output);
        Assert.Contains(
            $"Ending Position: ({destination.Destination.X}, {destination.Destination.Y})",
            output);
        Assert.Contains(
            $"Movement Spent Feet: {destination.MovementSpentFeet}",
            output);
        Assert.Contains(
            $"Random Values Consumed After: {expected.RandomValuesConsumedAfter}",
            output);

        for (int index = 0; index < destination.Path.Count; index++)
        {
            Assert.Contains(
                $"Path Position {index + 1}: ({destination.Path[index].X}, {destination.Path[index].Y})",
                output);
        }

        Assert.Equal(originalEncounter, CaptureEncounterState(encounter));
    }

    [Fact]
    public void RunSession_SelectedAttackUsesReturnedWeaponAndTarget()
    {
        using TemporaryDirectory temporary = new();
        AttackReadyState attackReady =
            CreateAttackReadyState();
        string originalEncounter =
            CaptureEncounterState(attackReady.Session);
        WatchtowerCombatResolutionResult normalized =
            WatchtowerCombatRules.AdvanceToDecision(
                attackReady.Session);
        WatchtowerCombatDecision decision =
            normalized.ResultingDecision;
        WatchtowerCombatTargetOption target =
            decision.WeaponAttack!.Targets
                .First(candidate => candidate.IsAvailable);
        WatchtowerCombatResolutionResult expected =
            WatchtowerCombatRules.Execute(
                normalized.State,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId =
                        decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });
        int selection = GetMovementCount(decision)
            + GetAvailableTargets(decision).ToList()
                .IndexOf(target)
            + 1;

        string output = RunSession(
            $"{selection}\n",
            temporary.SavePath,
            attackReady.Session);

        Assert.Contains(
            "Submitted Intent Kind: WeaponAttack",
            output);
        Assert.Contains(
            $"Weapon ID: {decision.WeaponAttack.WeaponId}",
            output);
        Assert.Contains(
            $"Target ID: {target.TargetCombatantId}",
            output);
        Assert.Contains("Attack Roll Mode:", output);
        Assert.Contains("Attack Outcome:", output);
        Assert.Contains("Final Damage:", output);
        Assert.Contains(
            $"Resulting Encounter Revision: {expected.ResultingEncounterRevision}",
            output);
        Assert.Contains(
            $"Random Values Consumed After: {expected.RandomValuesConsumedAfter}",
            output);
        Assert.Equal(
            originalEncounter,
            CaptureEncounterState(attackReady.Session));
    }

    [Fact]
    public void RunSession_AttackDiceRenderOnceAndInReturnedOrder()
    {
        using TemporaryDirectory temporary = new();
        AttackReadyState attackReady =
            CreateAttackReadyState();
        WatchtowerCombatResolutionResult normalized =
            WatchtowerCombatRules.AdvanceToDecision(
                attackReady.Session);
        WatchtowerCombatDecision decision =
            normalized.ResultingDecision;
        WatchtowerCombatTargetOption target =
            decision.WeaponAttack!.Targets
                .First(candidate => candidate.IsAvailable);
        WatchtowerCombatResolutionResult expected =
            WatchtowerCombatRules.Execute(
                normalized.State,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId =
                        decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });
        int selection = GetMovementCount(decision)
            + GetAvailableTargets(decision).ToList()
                .IndexOf(target)
            + 1;

        string output = RunSession(
            $"{selection}\n",
            temporary.SavePath,
            attackReady.Session);
        IReadOnlyList<WatchtowerCombatDieRoll> dice =
            expected.PrimaryStep!.Dice;

        int previous = -1;

        foreach (WatchtowerCombatDieRoll die in dice)
        {
            string line =
                $"Die {die.Ordinal}: Purpose={die.Purpose}, Sides={die.Sides}, Value={die.Value}";
            int index = output.IndexOf(
                line,
                StringComparison.Ordinal);

            Assert.True(index > previous);
            Assert.Equal(1, CountOccurrences(output, line));
            previous = index;
        }
    }

    [Fact]
    public void RunSession_EndTurnUsesReturnedActorAndRevision()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        WatchtowerCombatResolutionResult normalized =
            WatchtowerCombatRules.AdvanceToDecision(encounter);
        WatchtowerCombatDecision decision =
            normalized.ResultingDecision;
        int selection = GetEndTurnSelection(decision);
        string originalEncounter = CaptureEncounterState(encounter);
        WatchtowerCombatResolutionResult expected =
            WatchtowerCombatRules.Execute(
                normalized.State,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!
                });

        string output = RunSession(
            $"{selection}\n",
            temporary.SavePath,
            encounter);

        Assert.Contains("Submitted Intent Kind: EndTurn", output);
        Assert.Contains(
            $"Submitted Intent Actor ID: {decision.ActiveCombatantId}",
            output);
        Assert.Contains("Primary Step", output);
        Assert.Contains("Step Kind: TurnAdvanced", output);
        Assert.Contains(
            $"Resulting Encounter Revision: {expected.ResultingEncounterRevision}",
            output);
        Assert.Equal(originalEncounter, CaptureEncounterState(encounter));
    }

    [Fact]
    public void RunSession_AutomaticStepsRenderAfterPrimaryInOrder()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        WatchtowerCombatResolutionResult normalized =
            WatchtowerCombatRules.AdvanceToDecision(encounter);
        WatchtowerCombatDecision decision =
            normalized.ResultingDecision;
        WatchtowerCombatResolutionResult expected =
            WatchtowerCombatRules.Execute(
                normalized.State,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!
                });

        string output = RunSession(
            $"{GetEndTurnSelection(decision)}\n",
            temporary.SavePath,
            encounter);
        int primaryIndex = output.LastIndexOf(
            "Primary Step",
            StringComparison.Ordinal);
        int previous = primaryIndex;

        Assert.True(primaryIndex >= 0);

        for (int index = 0;
            index < expected.AutomaticSteps.Count;
            index++)
        {
            int automaticIndex = output.IndexOf(
                $"Automatic Step {index + 1}",
                previous + 1,
                StringComparison.Ordinal);

            Assert.True(automaticIndex > previous);
            previous = automaticIndex;
        }
    }

    [Fact]
    public void RunSession_InspectEncounterUsesCurrentEncounterAuthority()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        WatchtowerCombatResolutionResult normalized =
            WatchtowerCombatRules.AdvanceToDecision(encounter);
        int inspectSelection =
            GetInspectSelection(normalized.ResultingDecision);

        string output = RunSession(
            $"{inspectSelection}\n",
            temporary.SavePath,
            encounter);
        var authoritativeEncounter =
            normalized.State.ActiveEncounter!.Encounter;

        Assert.Contains("Encounter Inspection", output);

        int previous = -1;

        for (int index = 0;
            index < authoritativeEncounter.Participants.Count;
            index++)
        {
            var participant =
                authoritativeEncounter.Participants[index];
            string label = $"Participant {index + 1}";
            int participantIndex = output.IndexOf(
                label,
                StringComparison.Ordinal);

            Assert.True(participantIndex > previous);
            Assert.Contains(
                $"Combatant ID: {participant.Combatant.CombatantId}",
                output);
            Assert.Contains(
                $"Lifecycle State: {participant.Combatant.LifecycleState}",
                output);
            Assert.Contains(
                $"Hit Points: {participant.Combatant.Health.HitPoints.CurrentHitPoints} / {participant.Combatant.Health.HitPoints.MaximumHitPoints}",
                output);
            previous = participantIndex;
        }
    }

    [Fact]
    public void RunSession_InspectEncounterDoesNotAdvanceOrConsumeRandomness()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        WatchtowerCombatResolutionResult normalized =
            WatchtowerCombatRules.AdvanceToDecision(encounter);
        int inspectSelection =
            GetInspectSelection(normalized.ResultingDecision);
        string input = $"{inspectSelection}\n{inspectSelection}\n";
        int originalRandomValuesConsumed =
            encounter.RandomValuesConsumed;

        string output = RunSession(
            input,
            temporary.SavePath,
            encounter);

        Assert.Equal(
            1,
            CountOccurrences(output, "Combat Resolution"));
        Assert.Equal(
            2,
            CountOccurrences(output, "Encounter Inspection"));

        Assert.Equal(
            normalized.State.RandomValuesConsumed,
            normalized.RandomValuesConsumedAfter);
        Assert.Equal(
            originalRandomValuesConsumed,
            encounter.RandomValuesConsumed);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("999")]
    public void RunSession_InvalidCombatSelectionExecutesNoIntent(
        string invalidSelection)
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        string originalEncounter = CaptureEncounterState(encounter);

        string output = RunSession(
            $"{invalidSelection}\n",
            temporary.SavePath,
            encounter);

        Assert.Contains("Invalid selection.", output);
        Assert.Equal(
            1,
            CountOccurrences(output, "Combat Resolution"));
        Assert.Equal(
            2,
            CountOccurrences(output, "Combat Decision"));
        Assert.DoesNotContain("Submitted Intent Kind:", output);
        Assert.Equal(originalEncounter, CaptureEncounterState(encounter));
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void RunSession_EofAtCombatMenuExitsWithoutIntentOrSave()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateEncounterSession());

        Assert.Contains("Combat Menu", output);
        Assert.DoesNotContain("Submitted Intent Kind:", output);
        Assert.DoesNotContain("Combat Outcome", output);
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void RunSession_ExplicitCombatExitIsLastAndCreatesNoSave()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(encounter)
                .ResultingDecision;
        int exitSelection = GetExitSelection(decision);

        string output = RunSession(
            $"{exitSelection}\n",
            temporary.SavePath,
            encounter);
        string menu = GetLastCombatMenu(output);

        Assert.Contains(
            $"{exitSelection}. Exit",
            menu);
        Assert.DoesNotContain("Save", menu);
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void RunSession_PartyVictoryFinalizesExactlyOnceAndResumesExploration()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        CombatScript script = CreateCombatScript(
            encounter,
            CombatStrategy.AttackWhenPossible);
        int inspectSelection =
            GetNoncombatInspectSelection(script.Outcome.State);
        int exitSelection =
            GetNoncombatExitSelection(script.Outcome.State);
        string input = string.Join(
            '\n',
            script.Selections
                .Append(inspectSelection.ToString())
                .Append(exitSelection.ToString())
                .Append(string.Empty)
                .ToArray());

        string output = RunSession(
            input,
            temporary.SavePath,
            encounter);

        Assert.Equal(
            WatchtowerCombatOutcome.PartyVictory,
            script.Outcome.Outcome);
        Assert.Equal(
            1,
            CountOccurrences(output, "Combat Outcome"));
        Assert.Contains("Outcome: PartyVictory", output);
        Assert.Contains("Mode: Exploration", output);
        Assert.Contains("Progress: RaidersDefeated", output);
        Assert.Contains("Manual Save Available: Yes", output);
        Assert.Contains("Party Inspection", output);
        Assert.DoesNotContain("Save", GetLastCombatMenuBeforeOutcome(output));
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void RunSession_PartyVictoryDisplaysProjectedPartyAndAmmunition()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        CombatScript script = CreateCombatScript(
            encounter,
            CombatStrategy.AttackWhenPossible);
        int inspectSelection =
            GetNoncombatInspectSelection(script.Outcome.State);
        int exitSelection =
            GetNoncombatExitSelection(script.Outcome.State);
        string input = string.Join(
            '\n',
            script.Selections
                .Append(inspectSelection.ToString())
                .Append(exitSelection.ToString())
                .Append(string.Empty)
                .ToArray());

        string output = RunSession(
            input,
            temporary.SavePath,
            encounter);

        foreach (var member in script.Outcome.State.Party.Members)
        {
            Assert.Contains(
                $"Party Member ID: {member.PartyMemberId}",
                output);
            Assert.Contains(
                $"Hit Points: {member.Health.HitPoints.CurrentHitPoints} / {member.Health.HitPoints.MaximumHitPoints}",
                output);

            if (member.Ammunition is not null)
            {
                Assert.Contains(
                    $"Ammunition Remaining: {member.Ammunition.RemainingQuantity}",
                    output);
            }
        }
    }

    [Fact]
    public void RunSession_PostVictorySaveUsesExistingPersistenceBehavior()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        CombatScript script = CreateCombatScript(
            encounter,
            CombatStrategy.AttackWhenPossible);
        int saveSelection =
            GetNoncombatSaveSelection(script.Outcome.State);
        int exitSelection =
            GetNoncombatExitSelection(script.Outcome.State);
        string input = string.Join(
            '\n',
            script.Selections
                .Append(saveSelection.ToString())
                .Append(exitSelection.ToString())
                .Append(string.Empty)
                .ToArray());

        string output = RunSession(
            input,
            temporary.SavePath,
            encounter);

        Assert.Contains("Game saved to", output);
        Assert.Equal(
            ManualSaveSerializer.Serialize(script.Outcome.State),
            File.ReadAllText(temporary.SavePath));
    }

    [Fact]
    public void RunSession_ScenarioDefeatFinalizesAndUsesTerminalMenu()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        CombatScript script = CreateCombatScript(
            encounter,
            CombatStrategy.EndTurnOnly);
        string input = string.Join(
            '\n',
            script.Selections
                .Append("1")
                .Append("3")
                .Append(string.Empty)
                .ToArray());

        string output = RunSession(
            input,
            temporary.SavePath,
            encounter);

        Assert.Equal(
            WatchtowerCombatOutcome.ScenarioDefeat,
            script.Outcome.Outcome);
        Assert.Contains("Outcome: ScenarioDefeat", output);
        Assert.Contains("Mode: ScenarioConclusion", output);
        Assert.Contains("Progress: PartyDefeated", output);
        Assert.Contains("Manual Save Available: Yes", output);
        Assert.Contains("Step Kind: DeathSavingThrow", output);
        Assert.Contains("Previous Lifecycle:", output);
        Assert.Contains("Resulting Lifecycle:", output);
        Assert.Contains("Death Saving Throw Outcome:", output);
        Assert.Contains("Resulting Death Save Successes:", output);
        Assert.Contains("Resulting Death Save Failures:", output);
        Assert.Contains("Step Kind: CombatCompleted", output);
        AssertMenuOrder(
            GetLastSessionMenu(output),
            ["Inspect Party", "Save", "Exit"]);
        Assert.Contains("Party Inspection", output);

        foreach (var member in script.Outcome.State.Party.Members)
        {
            Assert.Contains(
                $"Party Member ID: {member.PartyMemberId}",
                output);
            Assert.Contains(
                $"Hit Points: {member.Health.HitPoints.CurrentHitPoints} / {member.Health.HitPoints.MaximumHitPoints}",
                output);
            Assert.Contains(
                $"Stable: {(member.Health.DeathSavingThrows.IsStable ? "Yes" : "No")}",
                output);
            Assert.Contains(
                $"Dead: {(member.Health.IsDead ? "Yes" : "No")}",
                output);
            Assert.Contains(
                $"Instant Death: {(member.Health.IsInstantlyDead ? "Yes" : "No")}",
                output);
            Assert.Contains(
                $"Death Save Successes: {member.Health.DeathSavingThrows.SuccessCount}",
                output);
            Assert.Contains(
                $"Death Save Failures: {member.Health.DeathSavingThrows.FailureCount}",
                output);
        }

        string lowerOutput = output.ToLowerInvariant();
        Assert.DoesNotContain("capture", lowerOutput);
        Assert.DoesNotContain("rescue", lowerOutput);
        Assert.DoesNotContain("revival", lowerOutput);
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void RunSession_FinalizationConsumesNoAdditionalRandomness()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        CombatScript script = CreateCombatScript(
            encounter,
            CombatStrategy.AttackWhenPossible);
        string input = string.Join(
            '\n',
            script.Selections
                .Append(GetNoncombatExitSelection(
                    script.Outcome.State).ToString())
                .Append(string.Empty)
                .ToArray());

        _ = RunSession(
            input,
            temporary.SavePath,
            encounter);

        Assert.Equal(
            script.CompletedSession.RandomValuesConsumed,
            script.Outcome.State.RandomValuesConsumed);
    }

    [Fact]
    public void Run_CompleteWalkingSkeletonReachesFinalizedPartyVictory()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        CombatScript combat = CreateCombatScript(
            encounter,
            CombatStrategy.AttackWhenPossible);
        List<string> selections =
        [
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
            "4"
        ];
        selections.AddRange(combat.Selections);
        selections.Add(
            GetNoncombatInspectSelection(combat.Outcome.State)
                .ToString());
        selections.Add(
            GetNoncombatExitSelection(combat.Outcome.State)
                .ToString());
        selections.Add(string.Empty);

        (int exitCode, string output) = Run(
            string.Join('\n', selections.ToArray()),
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(0, exitCode);
        Assert.Contains("Progress: MissionNotAccepted", output);
        Assert.Contains("Mode: RegionalTravel", output);
        Assert.Contains("Floor: GroundFloor", output);
        Assert.Contains("Floor: UpperFloor", output);
        Assert.Contains("Combat Outcome", output);
        Assert.Contains("Outcome: PartyVictory", output);
        Assert.Contains("Mode: Exploration", output);
        Assert.Contains("Progress: RaidersDefeated", output);
        Assert.Contains("Party Inspection", output);
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void RunSession_RepeatedEquivalentCombatScriptsProduceIdenticalOutput()
    {
        using TemporaryDirectory firstTemporary = new();
        using TemporaryDirectory secondTemporary = new();
        ApplicationSessionState encounter =
            CreateEncounterSession();
        CombatScript script = CreateCombatScript(
            encounter,
            CombatStrategy.AttackWhenPossible);
        string input = string.Join(
            '\n',
            script.Selections
                .Append(GetNoncombatExitSelection(
                    script.Outcome.State).ToString())
                .Append(string.Empty)
                .ToArray());

        string first = RunSession(
            input,
            firstTemporary.SavePath,
            encounter);
        string second = RunSession(
            input,
            secondTemporary.SavePath,
            CreateEncounterSession());

        Assert.Equal(first, second);
    }

    private static CombatScript CreateCombatScript(
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

                return new CombatScript(
                    selections,
                    state,
                    outcome);
            }

            Assert.Equal(
                WatchtowerCombatDecisionState.PlayerDecisionRequired,
                decision.State);

            IReadOnlyList<WatchtowerCombatMovementDestinationOption>
                movements = GetAvailableMovements(decision);
            IReadOnlyList<WatchtowerCombatTargetOption> targets =
                GetAvailableTargets(decision);

            if (strategy == CombatStrategy.AttackWhenPossible
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
                int targetIndex = targets.ToList().IndexOf(target);
                int selection = movements.Count + targetIndex + 1;
                selections.Add(selection.ToString());
                result = WatchtowerCombatRules.Execute(
                    state,
                    new WatchtowerCombatWeaponAttackIntent
                    {
                        ExpectedEncounterRevision =
                            decision.EncounterRevision,
                        ActorCombatantId =
                            decision.ActiveCombatantId!,
                        WeaponId =
                            decision.WeaponAttack!.WeaponId,
                        TargetCombatantId =
                            target.TargetCombatantId
                    });
            }
            else if (strategy
                == CombatStrategy.AttackWhenPossible
                && movements.Count > 0)
            {
                WatchtowerCombatMovementDestinationOption movement =
                    SelectMovementTowardNearestTarget(
                        state,
                        decision,
                        movements);
                int selection = movements.ToList()
                    .IndexOf(movement) + 1;
                selections.Add(selection.ToString());
                result = WatchtowerCombatRules.Execute(
                    state,
                    new WatchtowerCombatMoveIntent
                    {
                        ExpectedEncounterRevision =
                            decision.EncounterRevision,
                        ActorCombatantId =
                            decision.ActiveCombatantId!,
                        Path = movement.Path
                    });
            }
            else
            {
                int selection = GetEndTurnSelection(decision);
                selections.Add(selection.ToString());
                result = WatchtowerCombatRules.Execute(
                    state,
                    new WatchtowerCombatEndTurnIntent
                    {
                        ExpectedEncounterRevision =
                            decision.EncounterRevision,
                        ActorCombatantId =
                            decision.ActiveCombatantId!
                    });
            }

            state = result.State;
            decision = result.ResultingDecision;
        }

        throw new InvalidOperationException(
            "The deterministic combat script did not complete.");
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

    private static AttackReadyState CreateAttackReadyState()
    {
        ApplicationSessionState encounter =
            CreateEncounterSession();
        WatchtowerCombatResolutionResult normalized =
            WatchtowerCombatRules.AdvanceToDecision(encounter);

        foreach (WatchtowerCombatMovementDestinationOption movement
            in normalized.ResultingDecision.Movement!
                .DestinationOptions)
        {
            WatchtowerCombatResolutionResult moved =
                WatchtowerCombatRules.Execute(
                    normalized.State,
                    new WatchtowerCombatMoveIntent
                    {
                        ExpectedEncounterRevision =
                            normalized.ResultingDecision
                                .EncounterRevision,
                        ActorCombatantId =
                            normalized.ResultingDecision
                                .ActiveCombatantId!,
                        Path = movement.Path
                    });

            if (moved.ResultingDecision.WeaponAttack!
                .Targets.Any(target => target.IsAvailable))
            {
                return new AttackReadyState(moved.State);
            }
        }

        throw new InvalidOperationException(
            "No public movement destination produced an attack-ready state.");
    }

    private static IReadOnlyList<string> CreateCombatLabels(
        WatchtowerCombatDecision decision)
    {
        List<string> labels = new();

        foreach (WatchtowerCombatMovementDestinationOption movement
            in GetAvailableMovements(decision))
        {
            labels.Add(
                $"Move to ({movement.Destination.X}, {movement.Destination.Y}) - {movement.MovementSpentFeet} ft");
        }

        foreach (WatchtowerCombatTargetOption target
            in GetAvailableTargets(decision))
        {
            string label =
                $"Attack {target.TargetCombatantId} with {decision.WeaponAttack!.WeaponId}";

            if (target.DistanceFeet is not null)
            {
                label += $" - {target.DistanceFeet} ft";
            }

            if (target.AttackRollMode is not null)
            {
                label += target.DistanceFeet is null
                    ? $" - {target.AttackRollMode}"
                    : $", {target.AttackRollMode}";
            }

            labels.Add(label);
        }

        if (decision.EndTurn!.IsAvailable)
        {
            labels.Add("End Turn");
        }

        labels.Add("Inspect Encounter");
        labels.Add("Exit");
        return labels.AsReadOnly();
    }

    private static IReadOnlyList<WatchtowerCombatMovementDestinationOption>
        GetAvailableMovements(
            WatchtowerCombatDecision decision)
    {
        return decision.Movement!.IsAvailable
            ? decision.Movement.DestinationOptions
            : Array.Empty<WatchtowerCombatMovementDestinationOption>();
    }

    private static IReadOnlyList<WatchtowerCombatTargetOption>
        GetAvailableTargets(
            WatchtowerCombatDecision decision)
    {
        return decision.WeaponAttack!.IsAvailable
            ? decision.WeaponAttack.Targets
                .Where(target => target.IsAvailable)
                .ToArray()
            : Array.Empty<WatchtowerCombatTargetOption>();
    }

    private static int GetMovementCount(
        WatchtowerCombatDecision decision)
    {
        return GetAvailableMovements(decision).Count;
    }

    private static int GetEndTurnSelection(
        WatchtowerCombatDecision decision)
    {
        Assert.True(decision.EndTurn!.IsAvailable);
        return GetAvailableMovements(decision).Count
            + GetAvailableTargets(decision).Count
            + 1;
    }

    private static int GetInspectSelection(
        WatchtowerCombatDecision decision)
    {
        return GetAvailableMovements(decision).Count
            + GetAvailableTargets(decision).Count
            + (decision.EndTurn!.IsAvailable ? 1 : 0)
            + 1;
    }

    private static int GetExitSelection(
        WatchtowerCombatDecision decision)
    {
        return GetInspectSelection(decision) + 1;
    }

    private static int GetNoncombatInspectSelection(
        ApplicationSessionState session)
    {
        return GetNoncombatGameplayOptionCount(session) + 1;
    }

    private static int GetNoncombatSaveSelection(
        ApplicationSessionState session)
    {
        Assert.True(ManualSaveSerializer.CanSerialize(session));
        return GetNoncombatInspectSelection(session) + 1;
    }

    private static int GetNoncombatExitSelection(
        ApplicationSessionState session)
    {
        return GetNoncombatInspectSelection(session)
            + (ManualSaveSerializer.CanSerialize(session) ? 2 : 1);
    }

    private static int GetNoncombatGameplayOptionCount(
        ApplicationSessionState session)
    {
        if (session.CurrentMode == ApplicationMode.Exploration)
        {
            int count = 3;

            if (ExplorationRules.CanUseStairs(session))
            {
                count++;
            }

            if (SignalMechanismRules.CanActivate(session))
            {
                count++;
            }

            return count;
        }

        return 0;
    }

    private static int GetParticipantCurrentHitPoints(
        ApplicationSessionState session,
        string combatantId)
    {
        return session.ActiveEncounter!.Encounter.Participants
            .Single(participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal))
            .Combatant.Health.HitPoints.CurrentHitPoints;
    }

    private static string CaptureEncounterState(
        ApplicationSessionState session)
    {
        var encounter = session.ActiveEncounter!.Encounter;
        List<string> values =
        [
            session.CurrentMode.ToString(),
            session.Scenario.Progress.ToString(),
            session.CurrentLocationId,
            session.RandomSeed.ToString(),
            session.RandomValuesConsumed.ToString(),
            encounter.EncounterId,
            encounter.Revision.ToString(),
            encounter.LifecycleState.ToString(),
            encounter.ActiveCombatantId,
            encounter.TurnState.RoundNumber.ToString(),
            encounter.WinningSideId ?? string.Empty
        ];

        foreach (var participant in encounter.Participants)
        {
            values.Add(participant.Combatant.CombatantId);
            values.Add(participant.SideId);
            values.Add(participant.Position.X.ToString());
            values.Add(participant.Position.Y.ToString());
            values.Add(participant.Combatant.LifecycleState.ToString());
            values.Add(
                participant.Combatant.Health.HitPoints
                    .CurrentHitPoints.ToString());
            values.Add(
                participant.Combatant.Health.HitPoints
                    .TemporaryHitPoints.ToString());
            values.Add(
                participant.Combatant.Health.DeathSavingThrows
                    .SuccessCount.ToString());
            values.Add(
                participant.Combatant.Health.DeathSavingThrows
                    .FailureCount.ToString());
            values.Add(
                participant.Combatant.Health.DeathSavingThrows
                    .IsStable.ToString());
            values.Add(
                participant.Combatant.Health.IsInstantlyDead
                    .ToString());

            foreach (var weapon in participant.CombatProfile.WeaponAttacks)
            {
                values.Add(weapon.WeaponId);
                values.Add(
                    weapon.AmmunitionQuantityAvailable?.ToString()
                    ?? string.Empty);
            }
        }

        return string.Join('|', values);
    }

    private static ApplicationSessionState CreateEncounterSession()
    {
        ApplicationSessionState current =
            WatchtowerScenarioSessionFactory.CreateNew(RandomSeed);
        current = OutpostMissionRules.Resolve(
            current,
            OutpostMissionChoice.AcceptMission)
        .State;
        current = RegionalTravelRules.BeginWatchtowerJourney(
            current);

        while (RegionalTravelRules.CanAdvance(current))
        {
            current = RegionalTravelRules.Advance(current)
                .State;
        }

        current = ExplorationRules.EnterWatchtower(current);
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.UseStairs(current);
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current).State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);
        return SignalMechanismRules.Activate(current);
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

    private static string GetLastCombatMenu(string output)
    {
        const string SelectionPrompt = "Selection: ";
        int start = output.LastIndexOf(
            "Combat Menu",
            StringComparison.Ordinal);

        Assert.True(start >= 0);

        int end = output.IndexOf(
            SelectionPrompt,
            start,
            StringComparison.Ordinal);

        Assert.True(end >= 0);
        return output[
            start..(end + SelectionPrompt.Length)];
    }

    private static string GetLastCombatMenuBeforeOutcome(
        string output)
    {
        int outcome = output.IndexOf(
            "Combat Outcome",
            StringComparison.Ordinal);
        Assert.True(outcome >= 0);
        string combatOutput = output[..outcome];
        return GetLastCombatMenu(combatOutput);
    }

    private static string GetLastSessionMenu(string output)
    {
        int start = output.LastIndexOf(
            "Session Menu",
            StringComparison.Ordinal);

        Assert.True(start >= 0);
        return output[start..];
    }

    private static void AssertMenuOrder(
        string menu,
        IReadOnlyList<string> labels)
    {
        int previous = -1;

        for (int index = 0; index < labels.Count; index++)
        {
            string numbered = $"{index + 1}. {labels[index]}";
            int found = menu.IndexOf(
                numbered,
                StringComparison.Ordinal);

            Assert.True(
                found > previous,
                $"Expected '{numbered}' after the prior option.{Environment.NewLine}{menu}");
            previous = found;
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

    private sealed record AttackReadyState(
        ApplicationSessionState Session);

    private sealed record CombatScript(
        IReadOnlyList<string> Selections,
        ApplicationSessionState CompletedSession,
        WatchtowerCombatOutcomeResult Outcome);

    private enum CombatStrategy
    {
        AttackWhenPossible,
        EndTurnOnly
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
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(
                    DirectoryPath,
                    recursive: true);
            }
        }
    }
}
