using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerCombatDecisionTests
{
    [Fact]
    public void AdvanceToDecision_WithConsciousPartyActor_ReturnsStructuredPlayerDecision()
    {
        ApplicationSessionState state =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(state);

        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.StartingDecision.State);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.NotNull(result.ResultingDecision.ActiveCombatantId);
        Assert.NotNull(result.ResultingDecision.Movement);
        Assert.NotNull(result.ResultingDecision.WeaponAttack);
        Assert.NotNull(result.ResultingDecision.EndTurn);
        Assert.True(result.ResultingDecision.EndTurn.IsAvailable);
        Assert.Empty(result.AutomaticSteps);
        Assert.Equal(
            result.RandomValuesConsumedBefore,
            result.RandomValuesConsumedAfter);
        Assert.Null(result.PrimaryStep);
        Assert.Null(result.SubmittedIntent);
    }

    [Fact]
    public void AdvanceToDecision_ExposesFixedWeaponAndAuthoritativeTargets()
    {
        ApplicationSessionState state =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(state);
        EncounterParticipantState actor =
            WatchtowerCombatTestData.GetParticipant(
                state,
                encounter.ActiveCombatantId);
        WeaponAttack expectedWeapon = Assert.Single(
            actor.CombatProfile.WeaponAttacks);

        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(state)
                .ResultingDecision;

        Assert.Equal(
            expectedWeapon.WeaponId,
            decision.WeaponAttack!.WeaponId);
        Assert.Equal(2, decision.WeaponAttack.Targets.Count);

        foreach (WatchtowerCombatTargetOption target
            in decision.WeaponAttack.Targets)
        {
            EncounterWeaponAttackPrerequisiteEvaluation expected =
                EncounterWeaponAttackPrerequisiteRules.Evaluate(
                    encounter,
                    actor.Combatant.CombatantId,
                    target.TargetCombatantId,
                    expectedWeapon.WeaponId);

            Assert.Equal(expected.IsLegal, target.IsAvailable);
            Assert.Equal(
                expected.UnavailabilityReason,
                target.UnavailabilityReason);
            Assert.Equal(expected.AttackRollMode, target.AttackRollMode);
            Assert.Equal(expected.DistanceFeet, target.DistanceFeet);
        }
    }

    [Fact]
    public void AdvanceToDecision_WhenRaiderStarts_ProcessesAutomatically()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(
            WatchtowerCombatDecisionState.AutomaticProcessingRequired,
            result.StartingDecision.State);
        Assert.NotEmpty(result.AutomaticSteps);
        Assert.NotEqual(
            WatchtowerCombatDecisionState.AutomaticProcessingRequired,
            result.ResultingDecision.State);
    }

    [Fact]
    public void AdvanceToDecision_WhenCompleted_IsIdempotent()
    {
        ApplicationSessionState source =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active = Assert.IsType<ActiveEncounterState>(
            source.ActiveEncounter);
        EncounterState completed = EncounterRules.Complete(
            active.Encounter,
            "side.party");
        source = WatchtowerCombatTestData.ReplaceEncounter(
            source,
            completed);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(
            WatchtowerCombatDecisionState.CombatCompleted,
            result.StartingDecision.State);
        Assert.Equal(
            WatchtowerCombatDecisionState.CombatCompleted,
            result.ResultingDecision.State);
        Assert.Equal("side.party", result.ResultingDecision.WinningSideId);
        Assert.Empty(result.AutomaticSteps);
        Assert.Equal(source.RandomValuesConsumed, result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void Decision_WhenRangerHasNoArrows_ReportsStructuredUnavailability()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        string rangerId = source.Party.Members[2].PartyMemberId;
        source = WatchtowerCombatTestData.AdvanceToCombatant(
            source,
            rangerId);
        EncounterParticipantState ranger =
            WatchtowerCombatTestData.GetParticipant(source, rangerId);
        WeaponAttack weapon = Assert.Single(ranger.CombatProfile.WeaponAttacks);
        ranger = ranger with
        {
            CombatProfile = ranger.CombatProfile with
            {
                WeaponAttacks =
                [
                    weapon with
                    {
                        AmmunitionQuantityAvailable = 0
                    }
                ]
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, ranger);

        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(source)
                .ResultingDecision;

        Assert.False(decision.WeaponAttack!.IsAvailable);
        Assert.All(
            decision.WeaponAttack.Targets,
            target => Assert.Equal(
                EncounterActionUnavailabilityReason.AmmunitionUnavailable,
                target.UnavailabilityReason));
        Assert.True(decision.Movement!.IsAvailable);
        Assert.True(decision.EndTurn!.IsAvailable);
    }

    [Theory]
    [InlineData(CompletedIntentKind.Move)]
    [InlineData(CompletedIntentKind.WeaponAttack)]
    [InlineData(CompletedIntentKind.EndTurn)]
    public void Execute_WhenCombatCompleted_RejectsEveryPlayerIntentWithoutStateOrDice(
        CompletedIntentKind intentKind)
    {
        ApplicationSessionState activeSource =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision activeDecision =
            WatchtowerCombatRules.AdvanceToDecision(activeSource)
                .ResultingDecision;
        WatchtowerCombatTargetOption target =
            activeDecision.WeaponAttack!.Targets.First(
                candidate => candidate.IsAvailable);
        EncounterState activeEncounter =
            WatchtowerCombatTestData.GetEncounter(activeSource);
        ApplicationSessionState source =
            WatchtowerCombatTestData.ReplaceEncounter(
                activeSource,
                EncounterRules.Complete(activeEncounter, "side.party"));
        EncounterState completedBefore =
            WatchtowerCombatTestData.GetEncounter(source);
        int cursorBefore = source.RandomValuesConsumed;
        PartyMemberStateSnapshot[] partyBefore = source.Party.Members
            .Select(member => new PartyMemberStateSnapshot(
                member.PartyMemberId,
                member.Health,
                member.Ammunition))
            .ToArray();
        ExplorationState returnContextBefore =
            source.ActiveEncounter!.ReturnContext;
        CompletedParticipantSnapshot[] participantsBefore =
            completedBefore.Participants
                .Select(participant => new CompletedParticipantSnapshot(
                    participant.Combatant.CombatantId,
                    participant.Position,
                    participant.TurnResources,
                    participant.Combatant.Health,
                    participant.CombatProfile.WeaponAttacks
                        .Select(weapon => new CompletedWeaponSnapshot(
                            weapon.WeaponId,
                            weapon.AmmunitionQuantityAvailable))
                        .ToArray()))
                .ToArray();

        Action operation = intentKind switch
        {
            CompletedIntentKind.Move => () =>
            {
                _ = WatchtowerCombatRules.Execute(
                    source,
                    new WatchtowerCombatMoveIntent
                    {
                        ExpectedEncounterRevision = completedBefore.Revision,
                        ActorCombatantId = completedBefore.ActiveCombatantId,
                        Path = [new GridPosition(2, 0)]
                    });
            },
            CompletedIntentKind.WeaponAttack => () =>
            {
                _ = WatchtowerCombatRules.Execute(
                    source,
                    new WatchtowerCombatWeaponAttackIntent
                    {
                        ExpectedEncounterRevision = completedBefore.Revision,
                        ActorCombatantId = activeDecision.ActiveCombatantId!,
                        WeaponId = activeDecision.WeaponAttack.WeaponId,
                        TargetCombatantId = target.TargetCombatantId
                    });
            },
            CompletedIntentKind.EndTurn => () =>
            {
                _ = WatchtowerCombatRules.Execute(
                    source,
                    new WatchtowerCombatEndTurnIntent
                    {
                        ExpectedEncounterRevision = completedBefore.Revision,
                        ActorCombatantId = completedBefore.ActiveCombatantId
                    });
            },
            _ => throw new InvalidOperationException(
                "Unsupported completed-intent test case.")
        };

        Assert.Throws<InvalidOperationException>(operation);

        Assert.Equal(ApplicationMode.Encounter, source.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            source.Scenario.Progress);
        Assert.Equal(returnContextBefore, source.ActiveEncounter!.ReturnContext);
        Assert.Equal(cursorBefore, source.RandomValuesConsumed);
        Assert.Equal(partyBefore.Length, source.Party.Members.Count);

        for (int index = 0; index < partyBefore.Length; index++)
        {
            Assert.Equal(
                partyBefore[index].PartyMemberId,
                source.Party.Members[index].PartyMemberId);
            Assert.Equal(
                partyBefore[index].Health,
                source.Party.Members[index].Health);
            Assert.Equal(
                partyBefore[index].Ammunition,
                source.Party.Members[index].Ammunition);
        }

        EncounterState completedAfter =
            WatchtowerCombatTestData.GetEncounter(source);
        Assert.Equal(completedBefore.Revision, completedAfter.Revision);
        Assert.Equal(completedBefore.ActiveCombatantId, completedAfter.ActiveCombatantId);
        Assert.Equal(EncounterLifecycleState.Completed, completedAfter.LifecycleState);
        Assert.Equal("side.party", completedAfter.WinningSideId);
        Assert.Equal(participantsBefore.Length, completedAfter.Participants.Count);

        for (int index = 0; index < participantsBefore.Length; index++)
        {
            CompletedParticipantSnapshot expected = participantsBefore[index];
            EncounterParticipantState actual = completedAfter.Participants[index];
            Assert.Equal(expected.CombatantId, actual.Combatant.CombatantId);
            Assert.Equal(expected.Position, actual.Position);
            Assert.Equal(expected.TurnResources, actual.TurnResources);
            Assert.Equal(expected.Health, actual.Combatant.Health);
            Assert.Equal(expected.Weapons.Length, actual.CombatProfile.WeaponAttacks.Count);

            for (int weaponIndex = 0;
                weaponIndex < expected.Weapons.Length;
                weaponIndex++)
            {
                Assert.Equal(
                    expected.Weapons[weaponIndex].WeaponId,
                    actual.CombatProfile.WeaponAttacks[weaponIndex].WeaponId);
                Assert.Equal(
                    expected.Weapons[weaponIndex].AmmunitionQuantityAvailable,
                    actual.CombatProfile.WeaponAttacks[weaponIndex]
                        .AmmunitionQuantityAvailable);
            }
        }
    }

    public enum CompletedIntentKind
    {
        Move,
        WeaponAttack,
        EndTurn
    }

    private sealed record PartyMemberStateSnapshot(
        string PartyMemberId,
        FiveEGoldBox.Core.Rules.CombatantHealthState Health,
        AmmunitionState? Ammunition);

    private sealed record CompletedParticipantSnapshot(
        string CombatantId,
        GridPosition Position,
        CombatTurnResources TurnResources,
        FiveEGoldBox.Core.Rules.CombatantHealthState Health,
        CompletedWeaponSnapshot[] Weapons);

    private sealed record CompletedWeaponSnapshot(
        string WeaponId,
        int? AmmunitionQuantityAvailable);
}
