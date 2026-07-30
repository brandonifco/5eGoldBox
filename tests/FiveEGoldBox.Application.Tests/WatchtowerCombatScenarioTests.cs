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
            WatchtowerScenario.ProgressOf(completed));
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
        // Movement was asserted here because the old scripted fight opened by
        // walking the fighter into reach. The driven fight attacks or passes
        // and never needs to move; player movement is covered by the movement
        // tests rather than incidentally by this one.
        Assert.True(resolution.PartyMeleeWeaponAttacks > 0);
        Assert.True(resolution.RangerWeaponAttacks > 0);
        Assert.True(resolution.RaiderWeaponAttacks > 0);
        Assert.True(
            Assert.Single(
                WatchtowerCombatTestData.GetParticipant(
                    completed,
                    "party-member.rogue")
                .CombatProfile.WeaponAttacks,
                candidate => candidate.WeaponId == "weapon.shortbow")
            .AmmunitionQuantityAvailable!.Value
                < persistentParty.Members[CampaignTestParty.ArcherIndex()]
                    .Ammunition!.RemainingQuantity);
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
        ApplicationSessionState source = ScenarioTriggerRules.Activate(
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
            WatchtowerScenario.ProgressOf(completed));
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

    /// Fights to a party victory by driving the decision surface rather than
    /// scripting a turn order.
    ///
    /// The scripted version named each actor, its weapon and its target, which
    /// pinned the roster instead of the scenario. Full-strength raiders, no
    /// nerf: a doc comment here once claimed the baseline party loses the
    /// straight damage race against them, which does not hold up — a driven
    /// party that actually closes distance on the ranged raider (this loop
    /// does, below) wins the fight in the very large majority of random
    /// seeds. That claim came from a different test's driver that never
    /// issued movement, mistaking "my script can't reach the target" for
    /// "the encounter is unbalanced."
    private static ScenarioResolution ResolvePartyVictoryScenario(
        ApplicationSessionState source)
    {
        return ResolveScenario(
            source,
            attackWhenPossible: true,
            new ScenarioMetrics());
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
                == CombatDecisionState.CombatCompleted)
            {
                return metrics.Complete(state);
            }

            WatchtowerCombatDecision decision =
                advanced.ResultingDecision;

            // Every carried weapon offers its own targets, so the driver
            // picks across all of them rather than assuming one — an archer
            // with a bow and a dagger is exactly the case this has to cover.
            var attacks = decision.WeaponAttacks
                .Where(weapon => weapon.IsAvailable)
                .SelectMany(weapon => weapon.Targets
                    .Where(candidate => candidate.IsAvailable)
                    .Select(candidate => (weapon.WeaponId, Target: candidate)))
                .OrderBy(candidate =>
                    WatchtowerCombatTestData.GetParticipant(
                        state,
                        candidate.Target.TargetCombatantId)
                    .Combatant.Health.HitPoints.CurrentHitPoints)
                .ThenBy(candidate => candidate.Target.TargetCombatantId)
                .ToArray();

            if (attackWhenPossible && attacks.Length > 0)
            {
                WatchtowerCombatResolutionResult attacked =
                    WatchtowerCombatRules.Execute(
                        state,
                        new CombatWeaponAttackIntent
                        {
                            ExpectedEncounterRevision = decision.EncounterRevision,
                            ActorCombatantId = decision.ActiveCombatantId!,
                            WeaponId = attacks[0].WeaponId,
                            TargetCombatantId =
                                attacks[0].Target.TargetCombatantId
                        });
                metrics.Record(attacked);
                state = attacked.State;
                continue;
            }

            var outOfRange = decision.WeaponAttacks
                .SelectMany(weapon => weapon.Targets
                    .Where(candidate => candidate.UnavailabilityReason
                        == EncounterActionUnavailabilityReason
                            .TargetOutOfRange)
                    .Select(candidate =>
                        (weapon.WeaponId, candidate.TargetCombatantId,
                            candidate.DistanceFeet)))
                .OrderBy(candidate => candidate.DistanceFeet)
                .ThenBy(candidate => candidate.TargetCombatantId)
                .ToArray();

            if (attackWhenPossible
                && decision.Movement!.IsAvailable
                && outOfRange.Length > 0)
            {
                EncounterState encounter =
                    WatchtowerCombatTestData.GetEncounter(state);
                EncounterParticipantState actor =
                    WatchtowerCombatTestData.GetParticipant(
                        state,
                        decision.ActiveCombatantId!);
                EncounterMovementResult? movement =
                    WatchtowerCombatPathSearch.FindMovement(
                        encounter,
                        actor.Combatant.CombatantId,
                        outOfRange[0].TargetCombatantId,
                        outOfRange[0].WeaponId);

                if (movement is not null)
                {
                    WatchtowerCombatResolutionResult moved =
                        WatchtowerCombatRules.Execute(
                            state,
                            new CombatMoveIntent
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
                    new CombatEndTurnIntent
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

            if (step.Kind == CombatStepKind.Movement
                && step.ActorCombatantId?.StartsWith(
                    "party-member.",
                    StringComparison.Ordinal) == true)
            {
                PlayerMovementSteps++;
            }

            if (step.Kind == CombatStepKind.WeaponAttack)
            {
                if (string.Equals(
                    step.ActorCombatantId,
                    "party-member.rogue",
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

            if (step.Kind == CombatStepKind.DeathSavingThrow)
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
