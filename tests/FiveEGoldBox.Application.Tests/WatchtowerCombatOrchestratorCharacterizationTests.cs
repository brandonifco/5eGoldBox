using System.Globalization;
using System.Text;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

/// Characterization tests recorded before the Phase 5 decomposition of
/// WatchtowerCombatOrchestrator. They assert the orchestrator's observable
/// transcript rather than any internal structure, so the coming extraction of
/// policy, action-resolution, and outcome-mapping components can be verified as
/// behaviour-preserving. A failure here means the refactor changed behaviour,
/// not that these expectations need updating.
public sealed class WatchtowerCombatOrchestratorCharacterizationTests
{
    private const int OperationLimit = 80;

    /// An attacking party reaches its authored victory. Locks player command
    /// resolution, attack and damage dice ordinals, split movement after an
    /// action, and the completion step appended once combat ends.
    [Fact]
    public void AggressiveScript_MatchesFrozenTranscript()
    {
        ScriptedRun run = RunScriptedCombat(
            WatchtowerSignalTestData.CreateEncounterSession(),
            aggressive: true);

        AssertMatchesFrozenTranscript(
            "watchtower-combat-aggressive-transcript.txt",
            run.Lines);

        Assert.Equal(
            WatchtowerCombatDecisionState.CombatCompleted,
            run.Result.ResultingDecision.State);
        Assert.Equal(
            WatchtowerSignalEncounter.PartySideId,
            run.Result.ResultingDecision.WinningSideId);
    }

    /// A passive party is overrun. Locks raider target selection, movement into
    /// attack range, death-saving-throw resolution and stabilisation, the
    /// turn-advance reason recorded for every automatic step, and the raider
    /// victory that lands on a *successful* save once no conscious party member
    /// remains.
    [Fact]
    public void PassiveScript_MatchesFrozenTranscript()
    {
        ScriptedRun run = RunScriptedCombat(
            CreateFragilePartyEncounterSession(),
            aggressive: false);

        AssertMatchesFrozenTranscript(
            "watchtower-combat-passive-transcript.txt",
            run.Lines);

        Assert.Equal(
            WatchtowerCombatDecisionState.CombatCompleted,
            run.Result.ResultingDecision.State);
        Assert.Equal(
            WatchtowerSignalEncounter.RaiderSideId,
            run.Result.ResultingDecision.WinningSideId);
    }

    /// Each intent kind produces a receipt carrying exactly the submitted
    /// command back to the caller, including the fields that stay null or empty
    /// for that kind.
    [Fact]
    public void SubmittedIntentReceipts_EchoEachIntentKind()
    {
        ApplicationSessionState state =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(state).ResultingDecision;
        string actorId = Assert.IsType<string>(decision.ActiveCombatantId);
        WatchtowerCombatWeaponAttackOption weapon =
            Assert.IsType<WatchtowerCombatWeaponAttackOption>(decision.WeaponAttack);
        WatchtowerCombatTargetOption target = Assert.Single(
            weapon.Targets,
            candidate => candidate.IsAvailable);
        IReadOnlyList<GridPosition> path = Assert.IsType<
            WatchtowerCombatMovementOption>(decision.Movement)
            .DestinationOptions[0].Path;

        WatchtowerCombatIntentReceipt attack = RequireReceipt(
            WatchtowerCombatRules.Execute(
                state,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = actorId,
                    WeaponId = weapon.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                }));

        Assert.Equal(WatchtowerCombatIntentKind.WeaponAttack, attack.Kind);
        Assert.Equal(decision.EncounterRevision, attack.ExpectedEncounterRevision);
        Assert.Equal(actorId, attack.ActorCombatantId);
        Assert.Equal(weapon.WeaponId, attack.WeaponId);
        Assert.Equal(target.TargetCombatantId, attack.TargetCombatantId);
        Assert.Empty(attack.Path);

        WatchtowerCombatIntentReceipt move = RequireReceipt(
            WatchtowerCombatRules.Execute(
                state,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = actorId,
                    Path = path
                }));

        Assert.Equal(WatchtowerCombatIntentKind.Move, move.Kind);
        Assert.Equal(decision.EncounterRevision, move.ExpectedEncounterRevision);
        Assert.Equal(actorId, move.ActorCombatantId);
        Assert.Equal(path, move.Path);
        Assert.Null(move.WeaponId);
        Assert.Null(move.TargetCombatantId);

        WatchtowerCombatIntentReceipt endTurn = RequireReceipt(
            WatchtowerCombatRules.Execute(
                state,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = actorId
                }));

        Assert.Equal(WatchtowerCombatIntentKind.EndTurn, endTurn.Kind);
        Assert.Equal(decision.EncounterRevision, endTurn.ExpectedEncounterRevision);
        Assert.Equal(actorId, endTurn.ActorCombatantId);
        Assert.Empty(endTurn.Path);
        Assert.Null(endTurn.WeaponId);
        Assert.Null(endTurn.TargetCombatantId);
    }

    /// AdvanceToDecision reports no submitted intent, since no player command
    /// produced it.
    [Fact]
    public void AdvanceToDecision_ReportsNoSubmittedIntent()
    {
        Assert.Null(
            WatchtowerCombatRules.AdvanceToDecision(
                WatchtowerSignalTestData.CreateEncounterSession())
            .SubmittedIntent);
    }

    /// A finished run records exactly one completion step and never reports a
    /// winner before combat ends.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedRun_AppendsExactlyOneCompletionStep(bool aggressive)
    {
        ScriptedRun run = RunScriptedCombat(
            aggressive
                ? WatchtowerSignalTestData.CreateEncounterSession()
                : CreateFragilePartyEncounterSession(),
            aggressive);

        WatchtowerCombatStepResult[] steps = run.Steps.ToArray();
        WatchtowerCombatStepResult completion = Assert.Single(
            steps,
            step => step.Kind == WatchtowerCombatStepKind.CombatCompleted);

        Assert.Same(steps[^1], completion);
        Assert.All(
            steps[..^2],
            step => Assert.Null(step.WinningSideId));
    }

    /// Every operation chains onto the previous one: revisions and the random
    /// cursor only ever move forward, and each result's envelope agrees with the
    /// steps it reports.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EveryOperation_ChainsRevisionsAndRandomCursorForward(bool aggressive)
    {
        ScriptedRun run = RunScriptedCombat(
            aggressive
                ? WatchtowerSignalTestData.CreateEncounterSession()
                : CreateFragilePartyEncounterSession(),
            aggressive);

        long revision = -1;
        int cursor = -1;

        foreach (WatchtowerCombatResolutionResult result in run.Results)
        {
            if (revision >= 0)
            {
                Assert.Equal(revision, result.PriorEncounterRevision);
                Assert.Equal(cursor, result.RandomValuesConsumedBefore);
            }

            Assert.True(result.ResultingEncounterRevision >= result.PriorEncounterRevision);
            Assert.True(result.RandomValuesConsumedAfter >= result.RandomValuesConsumedBefore);
            Assert.NotEqual(
                WatchtowerCombatDecisionState.AutomaticProcessingRequired,
                result.ResultingDecision.State);

            int diceInSteps = CollectSteps(result).Sum(step => step.Dice.Count);
            Assert.Equal(
                result.RandomValuesConsumedAfter - result.RandomValuesConsumedBefore,
                diceInSteps);

            revision = result.ResultingEncounterRevision;
            cursor = result.RandomValuesConsumedAfter;
        }

        Assert.Equal(revision, WatchtowerCombatTestData.GetEncounter(run.State).Revision);
        Assert.Equal(cursor, run.State.RandomValuesConsumed);
    }

    private static WatchtowerCombatIntentReceipt RequireReceipt(
        WatchtowerCombatResolutionResult result)
    {
        return Assert.IsType<WatchtowerCombatIntentReceipt>(result.SubmittedIntent);
    }

    private static IEnumerable<WatchtowerCombatStepResult> CollectSteps(
        WatchtowerCombatResolutionResult result)
    {
        if (result.PrimaryStep is not null)
        {
            yield return result.PrimaryStep;
        }

        foreach (WatchtowerCombatStepResult step in result.AutomaticSteps)
        {
            yield return step;
        }
    }

    private static void AssertMatchesFrozenTranscript(
        string fixtureFileName,
        IReadOnlyList<string> actual)
    {
        string[] expected = File.ReadAllLines(
            Path.Combine(
                AppContext.BaseDirectory,
                "Fixtures",
                fixtureFileName));

        Assert.Equal(
            string.Join(Environment.NewLine, expected),
            string.Join(Environment.NewLine, actual));
    }

    /// The authored party at full health survives far longer than a readable
    /// transcript allows, so the passive run starts every member at 1 hit point.
    /// Raider attacks then drop the party quickly and the automatic death-saving
    /// throw and raider-victory paths are exercised in a compact recording.
    private static ApplicationSessionState CreateFragilePartyEncounterSession()
    {
        ApplicationSessionState signalReady =
            WatchtowerSignalTestData.CreateSignalReadySession();
        PartyMemberState[] members = signalReady.Party.Members
            .Select(member => member with
            {
                Health = member.Health with
                {
                    HitPoints = member.Health.HitPoints with
                    {
                        CurrentHitPoints = 1,
                        TemporaryHitPoints = 0
                    }
                }
            })
            .ToArray();

        return SignalMechanismRules.Activate(
            signalReady with
            {
                Party = signalReady.Party with
                {
                    Members = Array.AsReadOnly(members)
                }
            },
            WatchtowerSignalTestData.CreateRuleset());
    }

    private static ScriptedRun RunScriptedCombat(
        ApplicationSessionState source,
        bool aggressive)
    {
        ApplicationSessionState state = source;
        List<string> lines = [];
        List<WatchtowerCombatResolutionResult> results = [];

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(state);
        AppendOperation(lines, "AdvanceToDecision", result);
        results.Add(result);
        state = result.State;

        for (int operation = 0; operation < OperationLimit; operation++)
        {
            WatchtowerCombatDecision decision = result.ResultingDecision;

            if (decision.State != WatchtowerCombatDecisionState.PlayerDecisionRequired)
            {
                break;
            }

            result = SubmitScriptedIntent(
                state,
                decision,
                aggressive,
                lines);
            results.Add(result);
            state = result.State;
        }

        Assert.Equal(
            WatchtowerCombatDecisionState.CombatCompleted,
            result.ResultingDecision.State);

        return new ScriptedRun(
            lines,
            results,
            results.SelectMany(CollectSteps).ToArray(),
            state,
            result);
    }

    /// Deterministic player policy. The aggressive script attacks the first
    /// available target, otherwise steps onto the first offered destination,
    /// otherwise ends the turn. The passive script ends every turn immediately,
    /// handing each round to the raiders so the automatic enemy-turn and
    /// death-saving-throw paths are exercised without movement noise.
    private static WatchtowerCombatResolutionResult SubmitScriptedIntent(
        ApplicationSessionState state,
        WatchtowerCombatDecision decision,
        bool aggressive,
        List<string> lines)
    {
        string actorId = Assert.IsType<string>(decision.ActiveCombatantId);

        WatchtowerCombatTargetOption? target =
            aggressive
                && decision.WeaponAttack is { IsAvailable: true } weapon
                ? weapon.Targets.FirstOrDefault(candidate => candidate.IsAvailable)
                : null;

        if (target is not null && decision.WeaponAttack is not null)
        {
            WatchtowerCombatResolutionResult attack =
                WatchtowerCombatRules.Execute(
                    state,
                    new WatchtowerCombatWeaponAttackIntent
                    {
                        ExpectedEncounterRevision = decision.EncounterRevision,
                        ActorCombatantId = actorId,
                        WeaponId = decision.WeaponAttack.WeaponId,
                        TargetCombatantId = target.TargetCombatantId
                    });

            AppendOperation(lines, "Attack", attack);
            return attack;
        }

        WatchtowerCombatMovementDestinationOption? destination =
            aggressive
                && decision.Movement is { IsAvailable: true } movement
                ? movement.DestinationOptions.FirstOrDefault()
                : null;

        if (destination is not null)
        {
            WatchtowerCombatResolutionResult move =
                WatchtowerCombatRules.Execute(
                    state,
                    new WatchtowerCombatMoveIntent
                    {
                        ExpectedEncounterRevision = decision.EncounterRevision,
                        ActorCombatantId = actorId,
                        Path = destination.Path
                    });

            AppendOperation(lines, "Move", move);
            return move;
        }

        WatchtowerCombatResolutionResult endTurn =
            WatchtowerCombatRules.Execute(
                state,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = actorId
                });

        AppendOperation(lines, "EndTurn", endTurn);
        return endTurn;
    }

    private static void AppendOperation(
        List<string> lines,
        string label,
        WatchtowerCombatResolutionResult result)
    {
        lines.Add(string.Create(
            CultureInfo.InvariantCulture,
            $"== {label} rev {result.PriorEncounterRevision}->{result.ResultingEncounterRevision} cursor {result.RandomValuesConsumedBefore}->{result.RandomValuesConsumedAfter} next={result.ResultingDecision.State}"));

        if (result.PrimaryStep is not null)
        {
            lines.Add("   primary   " + RenderStep(result.PrimaryStep));
        }

        foreach (WatchtowerCombatStepResult step in result.AutomaticSteps)
        {
            lines.Add("   automatic " + RenderStep(step));
        }
    }

    private static string RenderStep(WatchtowerCombatStepResult step)
    {
        StringBuilder dice = new();

        foreach (WatchtowerCombatDieRoll die in step.Dice)
        {
            if (dice.Length > 0)
            {
                dice.Append(';');
            }

            dice.Append(string.Create(
                CultureInfo.InvariantCulture,
                $"#{die.Ordinal}d{die.Sides}={die.Value}:{die.Purpose}"));
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{step.Kind}|actor={step.ActorCombatantId ?? "-"}|target={step.TargetCombatantId ?? "-"}|reason={(step.TurnAdvanceReason?.ToString() ?? "-")}|dice={(dice.Length == 0 ? "-" : dice.ToString())}|rev={step.StartingEncounterRevision}->{step.ResultingEncounterRevision}|winner={step.WinningSideId ?? "-"}");
    }

    private sealed record ScriptedRun(
        IReadOnlyList<string> Lines,
        IReadOnlyList<WatchtowerCombatResolutionResult> Results,
        IReadOnlyList<WatchtowerCombatStepResult> Steps,
        ApplicationSessionState State,
        WatchtowerCombatResolutionResult Result);
}
