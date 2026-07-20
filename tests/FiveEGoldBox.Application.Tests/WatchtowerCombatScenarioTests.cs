using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerCombatScenarioTests
{
    [Fact]
    public void WatchtowerCombat_DefaultPartyCanReachAuthoritativePartyVictory()
    {
        ApplicationSessionState source =
            WatchtowerSignalTestData.CreateEncounterSession();
        PartyState persistentParty = source.Party;
        ExplorationState returnContext =
            source.ActiveEncounter!.ReturnContext;
        int cursorBefore = source.RandomValuesConsumed;

        ScenarioResolution resolution = ResolvePartyVictoryScenario(source);
        ApplicationSessionState completed = resolution.State;
        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(completed);

        Assert.Equal(EncounterLifecycleState.Completed, encounter.LifecycleState);
        Assert.Equal("side.party", encounter.WinningSideId);
        Assert.Equal(ApplicationMode.Encounter, completed.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            completed.Scenario.Progress);
        Assert.Equal(persistentParty.PartyId, completed.Party.PartyId);
        Assert.Equal(
            persistentParty.Members.ToArray(),
            completed.Party.Members.ToArray());
        Assert.Equal(returnContext, completed.ActiveEncounter!.ReturnContext);
        Assert.All(
            encounter.Participants.Where(participant =>
                participant.SideId == "side.watchtower-raiders"),
            participant => Assert.Equal(
                CombatantLifecycleState.Defeated,
                participant.Combatant.LifecycleState));
        Assert.True(resolution.PlayerMovementSteps >= 2);
        Assert.True(resolution.PartyMeleeWeaponAttacks > 0);
        Assert.True(resolution.RangerWeaponAttacks > 0);
        Assert.True(resolution.RaiderWeaponAttacks > 0);
        Assert.True(
            Assert.Single(
                WatchtowerCombatTestData.GetParticipant(
                    completed,
                    "party-member.ranger")
                .CombatProfile.WeaponAttacks)
            .AmmunitionQuantityAvailable!.Value
                < persistentParty.Members[2].Ammunition!.RemainingQuantity);
        Assert.True(resolution.PlayerEndTurns > 0);
        Assert.Equal(
            cursorBefore + resolution.GeneratedDice,
            completed.RandomValuesConsumed);
    }

    [Fact]
    public void WatchtowerCombat_PassivePartyReachesAuthoritativeRaiderVictoryThroughDeathSaves()
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
        signalReady = signalReady with
        {
            Party = signalReady.Party with
            {
                Members = Array.AsReadOnly(members)
            }
        };
        ApplicationSessionState source = SignalMechanismRules.Activate(
            signalReady,
            WatchtowerSignalTestData.CreateRuleset());
        PartyState persistentParty = source.Party;
        ExplorationState returnContext =
            source.ActiveEncounter!.ReturnContext;
        int cursorBefore = source.RandomValuesConsumed;

        ScenarioResolution resolution = ResolveScenario(
            source,
            attackWhenPossible: false,
            new ScenarioMetrics());
        ApplicationSessionState completed = resolution.State;
        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(completed);

        Assert.Equal(EncounterLifecycleState.Completed, encounter.LifecycleState);
        Assert.Equal("side.watchtower-raiders", encounter.WinningSideId);
        Assert.Equal(ApplicationMode.Encounter, completed.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            completed.Scenario.Progress);
        Assert.Null(completed.Exploration);
        Assert.NotNull(completed.ActiveEncounter);
        Assert.Equal(
            returnContext,
            completed.ActiveEncounter!.ReturnContext);
        Assert.Equal(persistentParty.PartyId, completed.Party.PartyId);
        Assert.Equal(
            persistentParty.Members.ToArray(),
            completed.Party.Members.ToArray());
        Assert.DoesNotContain(
            encounter.Participants.Where(participant =>
                participant.SideId == "side.party"),
            participant => participant.Combatant.LifecycleState
                is CombatantLifecycleState.Conscious
                or CombatantLifecycleState.Dying);
        Assert.True(resolution.DeathSavingThrows > 0);
        Assert.True(resolution.NoProductiveEnemyTurns > 0);
        Assert.True(resolution.RaiderWeaponAttacks > 0);
        Assert.Equal(
            cursorBefore + resolution.GeneratedDice,
            completed.RandomValuesConsumed);
    }

    private static ScenarioResolution ResolvePartyVictoryScenario(
        ApplicationSessionState source)
    {
        ScenarioMetrics metrics = new();
        WatchtowerCombatResolutionResult advanced =
            WatchtowerCombatRules.AdvanceToDecision(source);
        metrics.Record(advanced);
        ApplicationSessionState state = advanced.State;
        WatchtowerCombatDecision decision = advanced.ResultingDecision;

        Assert.Equal("party-member.fighter", decision.ActiveCombatantId);

        WatchtowerCombatResolutionResult movedBeforeAttack =
            WatchtowerCombatRules.Execute(
                state,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    Path = [new GridPosition(2, 0)]
                });
        metrics.Record(movedBeforeAttack);
        state = movedBeforeAttack.State;
        decision = movedBeforeAttack.ResultingDecision;

        WatchtowerCombatTargetOption meleeRaider = Assert.Single(
            decision.WeaponAttack!.Targets,
            target => target.TargetCombatantId
                == "combatant.watchtower-raider.melee");
        Assert.True(meleeRaider.IsAvailable);

        WatchtowerCombatResolutionResult attacked =
            WatchtowerCombatRules.Execute(
                state,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = meleeRaider.TargetCombatantId
                });
        metrics.Record(attacked);
        state = attacked.State;
        decision = attacked.ResultingDecision;

        WatchtowerCombatResolutionResult movedAfterAttack =
            WatchtowerCombatRules.Execute(
                state,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    Path = [new GridPosition(3, 0)]
                });
        metrics.Record(movedAfterAttack);
        state = movedAfterAttack.State;
        decision = movedAfterAttack.ResultingDecision;

        WatchtowerCombatResolutionResult ended =
            WatchtowerCombatRules.Execute(
                state,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!
                });
        metrics.Record(ended);
        state = ended.State;
        decision = ended.ResultingDecision;

        Assert.Equal("party-member.barbarian", decision.ActiveCombatantId);
        WatchtowerCombatResolutionResult barbarianPassed =
            WatchtowerCombatRules.Execute(
                state,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!
                });
        metrics.Record(barbarianPassed);

        return ResolveScenario(
            barbarianPassed.State,
            attackWhenPossible: true,
            metrics);
    }

    private static ScenarioResolution ResolveScenario(
        ApplicationSessionState source,
        bool attackWhenPossible,
        ScenarioMetrics metrics)
    {
        ApplicationSessionState state = source;

        for (int operation = 0; operation < 1000; operation++)
        {
            WatchtowerCombatResolutionResult advanced =
                WatchtowerCombatRules.AdvanceToDecision(state);
            metrics.Record(advanced);
            state = advanced.State;

            if (advanced.ResultingDecision.State
                == WatchtowerCombatDecisionState.CombatCompleted)
            {
                return metrics.Complete(state);
            }

            WatchtowerCombatDecision decision =
                advanced.ResultingDecision;

            if (attackWhenPossible
                && decision.WeaponAttack!.IsAvailable)
            {
                WatchtowerCombatTargetOption target =
                    decision.WeaponAttack.Targets
                        .Where(candidate => candidate.IsAvailable)
                        .OrderBy(candidate =>
                            WatchtowerCombatTestData.GetParticipant(
                                state,
                                candidate.TargetCombatantId)
                            .Combatant.Health.HitPoints.CurrentHitPoints)
                        .ThenBy(candidate => candidate.TargetCombatantId)
                        .First();

                WatchtowerCombatResolutionResult attacked =
                    WatchtowerCombatRules.Execute(
                        state,
                        new WatchtowerCombatWeaponAttackIntent
                        {
                            ExpectedEncounterRevision = decision.EncounterRevision,
                            ActorCombatantId = decision.ActiveCombatantId!,
                            WeaponId = decision.WeaponAttack.WeaponId,
                            TargetCombatantId = target.TargetCombatantId
                        });
                metrics.Record(attacked);
                state = attacked.State;
                continue;
            }

            if (attackWhenPossible
                && decision.Movement!.IsAvailable
                && decision.WeaponAttack!.Targets.Any(target =>
                    target.UnavailabilityReason
                        == EncounterActionUnavailabilityReason.TargetOutOfRange))
            {
                EncounterState encounter =
                    WatchtowerCombatTestData.GetEncounter(state);
                EncounterParticipantState actor =
                    WatchtowerCombatTestData.GetParticipant(
                        state,
                        decision.ActiveCombatantId!);
                string targetId = decision.WeaponAttack.Targets
                    .Where(target => target.UnavailabilityReason
                        == EncounterActionUnavailabilityReason.TargetOutOfRange)
                    .OrderBy(target => target.DistanceFeet)
                    .ThenBy(target => target.TargetCombatantId)
                    .First()
                    .TargetCombatantId;
                EncounterMovementResult? movement =
                    WatchtowerCombatPathSearch.FindMovement(
                        encounter,
                        actor.Combatant.CombatantId,
                        targetId,
                        decision.WeaponAttack.WeaponId);

                if (movement is not null)
                {
                    WatchtowerCombatResolutionResult moved =
                        WatchtowerCombatRules.Execute(
                            state,
                            new WatchtowerCombatMoveIntent
                            {
                                ExpectedEncounterRevision = decision.EncounterRevision,
                                ActorCombatantId = decision.ActiveCombatantId!,
                                Path = movement.Path
                            });
                    metrics.Record(moved);
                    state = moved.State;
                    continue;
                }
            }

            WatchtowerCombatResolutionResult ended =
                WatchtowerCombatRules.Execute(
                    state,
                    new WatchtowerCombatEndTurnIntent
                    {
                        ExpectedEncounterRevision = decision.EncounterRevision,
                        ActorCombatantId = decision.ActiveCombatantId!
                    });
            metrics.Record(ended);
            state = ended.State;
        }

        throw new InvalidOperationException(
            "The deterministic watchtower combat scenario did not complete.");
    }

    private sealed class ScenarioMetrics
    {
        internal int GeneratedDice { get; private set; }

        internal int PlayerMovementSteps { get; private set; }

        internal int PartyMeleeWeaponAttacks { get; private set; }

        internal int RangerWeaponAttacks { get; private set; }

        internal int RaiderWeaponAttacks { get; private set; }

        internal int PlayerEndTurns { get; private set; }

        internal int DeathSavingThrows { get; private set; }

        internal int NoProductiveEnemyTurns { get; private set; }

        internal void Record(WatchtowerCombatResolutionResult result)
        {
            if (result.PrimaryStep is not null)
            {
                RecordStep(result.PrimaryStep);
            }

            foreach (WatchtowerCombatStepResult step in result.AutomaticSteps)
            {
                RecordStep(step);
            }
        }

        internal ScenarioResolution Complete(ApplicationSessionState state)
        {
            return new ScenarioResolution(
                state,
                GeneratedDice,
                PlayerMovementSteps,
                PartyMeleeWeaponAttacks,
                RangerWeaponAttacks,
                RaiderWeaponAttacks,
                PlayerEndTurns,
                DeathSavingThrows,
                NoProductiveEnemyTurns);
        }

        private void RecordStep(WatchtowerCombatStepResult step)
        {
            GeneratedDice += step.Dice.Count;

            if (step.Kind == WatchtowerCombatStepKind.Movement
                && step.ActorCombatantId?.StartsWith(
                    "party-member.",
                    StringComparison.Ordinal) == true)
            {
                PlayerMovementSteps++;
            }

            if (step.Kind == WatchtowerCombatStepKind.WeaponAttack)
            {
                if (string.Equals(
                    step.ActorCombatantId,
                    "party-member.ranger",
                    StringComparison.Ordinal))
                {
                    RangerWeaponAttacks++;
                }
                else if (step.ActorCombatantId?.StartsWith(
                    "party-member.",
                    StringComparison.Ordinal) == true)
                {
                    PartyMeleeWeaponAttacks++;
                }
                else if (step.ActorCombatantId?.StartsWith(
                    "combatant.watchtower-raider.",
                    StringComparison.Ordinal) == true)
                {
                    RaiderWeaponAttacks++;
                }
            }

            if (step.TurnAdvanceReason
                == WatchtowerCombatTurnAdvanceReason.PlayerEndTurn)
            {
                PlayerEndTurns++;
            }

            if (step.Kind == WatchtowerCombatStepKind.DeathSavingThrow)
            {
                DeathSavingThrows++;
            }

            if (step.TurnAdvanceReason
                == WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction)
            {
                NoProductiveEnemyTurns++;
            }
        }
    }

    private sealed record ScenarioResolution(
        ApplicationSessionState State,
        int GeneratedDice,
        int PlayerMovementSteps,
        int PartyMeleeWeaponAttacks,
        int RangerWeaponAttacks,
        int RaiderWeaponAttacks,
        int PlayerEndTurns,
        int DeathSavingThrows,
        int NoProductiveEnemyTurns);
}
