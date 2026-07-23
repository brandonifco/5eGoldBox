using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerCombatAutomaticTurnTests
{
    [Fact]
    public void AdvanceToDecision_DyingParticipant_ResolvesDeathSaveAndAdvances()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreateDyingActiveSession(
                randomValuesConsumed: 10,
                successes: 1,
                failures: 0);
        string actorId = WatchtowerCombatTestData.GetEncounter(source)
            .ActiveCombatantId;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        WatchtowerCombatStepResult deathSave = result.AutomaticSteps[0];
        Assert.Equal(
            WatchtowerCombatStepKind.DeathSavingThrow,
            deathSave.Kind);
        Assert.Equal(actorId, deathSave.ActorCombatantId);
        WatchtowerCombatDieRoll die = Assert.Single(deathSave.Dice);
        Assert.Equal(20, die.Sides);
        Assert.Equal(5, die.Value);
        Assert.Equal(
            WatchtowerCombatDiePurpose.DeathSavingThrow,
            die.Purpose);
        Assert.Equal(
            WatchtowerCombatStepKind.TurnAdvanced,
            result.AutomaticSteps[1].Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.DyingParticipantAfterSave,
            result.AutomaticSteps[1].TurnAdvanceReason);
        Assert.Equal(
            source.RandomValuesConsumed + 1,
            deathSave.Dice[0].Ordinal);
        Assert.Equal(10, source.RandomValuesConsumed);
    }

    [Fact]
    public void AdvanceToDecision_NaturalOneDeathSave_AppliesTwoFailuresAndAdvances()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreateDyingActiveSession(
                randomValuesConsumed: 3,
                successes: 1,
                failures: 0);
        string actorId = WatchtowerCombatTestData.GetEncounter(source)
            .ActiveCombatantId;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(2, result.AutomaticSteps.Count);
        WatchtowerCombatStepResult deathSave = result.AutomaticSteps[0];
        WatchtowerCombatDieRoll die = Assert.Single(deathSave.Dice);
        EncounterDeathSavingThrowResult coreResult =
            Assert.IsType<EncounterDeathSavingThrowResult>(
                deathSave.DeathSavingThrow);
        DeathSavingThrowResult mechanicalResult =
            coreResult.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow;
        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(result.State, actorId);

        Assert.Equal(1, die.Value);
        Assert.Equal(20, die.Sides);
        Assert.Equal(
            WatchtowerCombatDiePurpose.DeathSavingThrow,
            die.Purpose);
        Assert.Equal(
            DeathSavingThrowOutcome.Failure,
            mechanicalResult.Outcome);
        Assert.Equal(1, mechanicalResult.State.SuccessCount);
        Assert.Equal(2, mechanicalResult.State.FailureCount);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            coreResult.LifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            participant.Combatant.LifecycleState);
        Assert.Equal(0, participant.Combatant.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            WatchtowerCombatStepKind.TurnAdvanced,
            result.AutomaticSteps[1].Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.DyingParticipantAfterSave,
            result.AutomaticSteps[1].TurnAdvanceReason);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.Equal(1, result.AutomaticSteps.Sum(step => step.Dice.Count));
        Assert.Equal(
            source.RandomValuesConsumed + 1,
            result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void AdvanceToDecision_OrdinarySuccessfulDeathSave_RecordsSuccessAndAdvances()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreateDyingActiveSession(
                randomValuesConsumed: 22,
                successes: 0,
                failures: 1);
        string actorId = WatchtowerCombatTestData.GetEncounter(source)
            .ActiveCombatantId;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(2, result.AutomaticSteps.Count);
        WatchtowerCombatStepResult deathSave = result.AutomaticSteps[0];
        WatchtowerCombatDieRoll die = Assert.Single(deathSave.Dice);
        EncounterDeathSavingThrowResult coreResult =
            Assert.IsType<EncounterDeathSavingThrowResult>(
                deathSave.DeathSavingThrow);
        DeathSavingThrowResult mechanicalResult =
            coreResult.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow;
        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(result.State, actorId);

        Assert.Equal(11, die.Value);
        Assert.Equal(20, die.Sides);
        Assert.Equal(
            WatchtowerCombatDiePurpose.DeathSavingThrow,
            die.Purpose);
        Assert.Equal(
            DeathSavingThrowOutcome.Success,
            mechanicalResult.Outcome);
        Assert.Equal(1, mechanicalResult.State.SuccessCount);
        Assert.Equal(1, mechanicalResult.State.FailureCount);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            coreResult.LifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            participant.Combatant.LifecycleState);
        Assert.Equal(0, participant.Combatant.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            WatchtowerCombatStepKind.TurnAdvanced,
            result.AutomaticSteps[1].Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.DyingParticipantAfterSave,
            result.AutomaticSteps[1].TurnAdvanceReason);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.Equal(1, result.AutomaticSteps.Sum(step => step.Dice.Count));
        Assert.Equal(
            source.RandomValuesConsumed + 1,
            result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void AdvanceToDecision_ThirdSuccessfulDeathSave_StabilizesAndAdvances()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreateDyingActiveSession(
                randomValuesConsumed: 22,
                successes: 2,
                failures: 1);
        string actorId = WatchtowerCombatTestData.GetEncounter(source)
            .ActiveCombatantId;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(2, result.AutomaticSteps.Count);
        WatchtowerCombatStepResult deathSave = result.AutomaticSteps[0];
        WatchtowerCombatDieRoll die = Assert.Single(deathSave.Dice);
        EncounterDeathSavingThrowResult coreResult =
            Assert.IsType<EncounterDeathSavingThrowResult>(
                deathSave.DeathSavingThrow);
        DeathSavingThrowResult mechanicalResult =
            coreResult.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow;
        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(result.State, actorId);

        Assert.Equal(11, die.Value);
        Assert.Equal(20, die.Sides);
        Assert.Equal(
            WatchtowerCombatDiePurpose.DeathSavingThrow,
            die.Purpose);
        Assert.Equal(
            DeathSavingThrowOutcome.Stabilized,
            mechanicalResult.Outcome);
        Assert.True(mechanicalResult.State.IsStable);
        Assert.Equal(0, mechanicalResult.State.SuccessCount);
        Assert.Equal(0, mechanicalResult.State.FailureCount);
        Assert.Equal(
            CombatantLifecycleState.Stable,
            coreResult.LifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Stable,
            participant.Combatant.LifecycleState);
        Assert.Equal(0, participant.Combatant.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            WatchtowerCombatStepKind.TurnAdvanced,
            result.AutomaticSteps[1].Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.DyingParticipantAfterSave,
            result.AutomaticSteps[1].TurnAdvanceReason);
        Assert.DoesNotContain(
            result.AutomaticSteps.Skip(1),
            step => step.Kind == WatchtowerCombatStepKind.DeathSavingThrow);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.Equal(1, result.AutomaticSteps.Sum(step => step.Dice.Count));
        Assert.Equal(
            source.RandomValuesConsumed + 1,
            result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void AdvanceToDecision_ThirdFailedDeathSave_KillsParticipantAndAdvances()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreateDyingActiveSession(
                randomValuesConsumed: 10,
                successes: 1,
                failures: 2);
        string actorId = WatchtowerCombatTestData.GetEncounter(source)
            .ActiveCombatantId;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(2, result.AutomaticSteps.Count);
        WatchtowerCombatStepResult deathSave = result.AutomaticSteps[0];
        WatchtowerCombatDieRoll die = Assert.Single(deathSave.Dice);
        EncounterDeathSavingThrowResult coreResult =
            Assert.IsType<EncounterDeathSavingThrowResult>(
                deathSave.DeathSavingThrow);
        DeathSavingThrowResult mechanicalResult =
            coreResult.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow;
        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(result.State, actorId);

        Assert.Equal(5, die.Value);
        Assert.Equal(20, die.Sides);
        Assert.Equal(
            WatchtowerCombatDiePurpose.DeathSavingThrow,
            die.Purpose);
        Assert.Equal(
            DeathSavingThrowOutcome.Dead,
            mechanicalResult.Outcome);
        Assert.Equal(1, mechanicalResult.State.SuccessCount);
        Assert.Equal(3, mechanicalResult.State.FailureCount);
        Assert.Equal(
            CombatantLifecycleState.Dead,
            coreResult.LifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dead,
            participant.Combatant.LifecycleState);
        Assert.Equal(0, participant.Combatant.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            WatchtowerCombatStepKind.TurnAdvanced,
            result.AutomaticSteps[1].Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.DyingParticipantAfterSave,
            result.AutomaticSteps[1].TurnAdvanceReason);
        Assert.Equal(
            EncounterLifecycleState.Active,
            WatchtowerCombatTestData.GetEncounter(result.State).LifecycleState);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.Equal(1, result.AutomaticSteps.Sum(step => step.Dice.Count));
        Assert.Equal(
            source.RandomValuesConsumed + 1,
            result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void AdvanceToDecision_FinalDeathSaveFailure_CompletesImmediatelyWithoutLaterWork()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreateDyingActiveSession(
                randomValuesConsumed: 10,
                successes: 1,
                failures: 2);
        string actorId = WatchtowerCombatTestData.GetEncounter(source)
            .ActiveCombatantId;

        foreach (string partyMemberId in source.Party.Members
            .Select(member => member.PartyMemberId)
            .Where(id => !string.Equals(id, actorId, StringComparison.Ordinal)))
        {
            EncounterParticipantState participant =
                WatchtowerCombatTestData.GetParticipant(
                    source,
                    partyMemberId);
            participant = participant with
            {
                Combatant = participant.Combatant with
                {
                    Health = WatchtowerCombatTestData.CreateZeroHealth(
                        participant.Combatant.Health.HitPoints.MaximumHitPoints,
                        successes: 0,
                        failures: 3,
                        isStable: false)
                }
            };
            source = WatchtowerCombatTestData.ReplaceParticipant(
                source,
                participant);
        }

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(2, result.AutomaticSteps.Count);
        WatchtowerCombatStepResult deathSave = result.AutomaticSteps[0];
        WatchtowerCombatDieRoll die = Assert.Single(deathSave.Dice);
        EncounterDeathSavingThrowResult coreResult =
            Assert.IsType<EncounterDeathSavingThrowResult>(
                deathSave.DeathSavingThrow);
        DeathSavingThrowResult mechanicalResult =
            coreResult.CombatantDeathSavingThrow
                .HealthDeathSavingThrow
                .DeathSavingThrow;
        EncounterState completed = WatchtowerCombatTestData.GetEncounter(
            result.State);
        EncounterParticipantState actorParticipant =
            WatchtowerCombatTestData.GetParticipant(result.State, actorId);

        Assert.Equal(5, die.Value);
        Assert.Equal(20, die.Sides);
        Assert.Equal(
            WatchtowerCombatDiePurpose.DeathSavingThrow,
            die.Purpose);
        Assert.Equal(
            DeathSavingThrowOutcome.Dead,
            mechanicalResult.Outcome);
        Assert.Equal(1, mechanicalResult.State.SuccessCount);
        Assert.Equal(3, mechanicalResult.State.FailureCount);
        Assert.Equal(
            CombatantLifecycleState.Dead,
            coreResult.LifecycleState);
        Assert.Equal(
            CombatantLifecycleState.Dead,
            actorParticipant.Combatant.LifecycleState);
        Assert.Equal(0, actorParticipant.Combatant.Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            EncounterLifecycleState.Completed,
            coreResult.State.LifecycleState);
        Assert.Equal(
            EncounterLifecycleState.Completed,
            completed.LifecycleState);
        Assert.Equal("side.watchtower-raiders", completed.WinningSideId);
        Assert.Equal(
            WatchtowerCombatStepKind.CombatCompleted,
            result.AutomaticSteps[1].Kind);
        Assert.Empty(result.AutomaticSteps[1].Dice);
        Assert.DoesNotContain(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.TurnAdvanced);
        Assert.Equal(
            WatchtowerCombatDecisionState.CombatCompleted,
            result.ResultingDecision.State);
        Assert.Equal(1, result.AutomaticSteps.Sum(step => step.Dice.Count));
        Assert.Equal(
            source.RandomValuesConsumed + 1,
            result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void AdvanceToDecision_NaturalTwentyDeathSave_ReturnsSameParticipantDecision()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreateDyingActiveSession(
                randomValuesConsumed: 35,
                successes: 1,
                failures: 0);
        EncounterState before = WatchtowerCombatTestData.GetEncounter(source);
        string actorId = before.ActiveCombatantId;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Single(result.AutomaticSteps);
        WatchtowerCombatStepResult step = result.AutomaticSteps[0];
        Assert.Equal(20, Assert.Single(step.Dice).Value);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            step.DeathSavingThrow!.LifecycleState);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.Equal(actorId, result.ResultingDecision.ActiveCombatantId);
        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(result.State, actorId);
        Assert.Equal(
            1,
            participant.Combatant.Health.HitPoints.CurrentHitPoints);
        Assert.True(participant.TurnResources.HasActionAvailable);
        Assert.Equal(0, participant.TurnResources.MovementSpentFeet);
        Assert.Equal(source.RandomValuesConsumed + 1, result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void AdvanceToDecision_StableParticipant_AutomaticallyAdvancesWithoutDice()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        string actorId = encounter.ActiveCombatantId;
        EncounterParticipantState actor =
            WatchtowerCombatTestData.GetParticipant(source, actorId);
        actor = actor with
        {
            Combatant = actor.Combatant with
            {
                Health = WatchtowerCombatTestData.CreateZeroHealth(
                    actor.Combatant.Health.HitPoints.MaximumHitPoints,
                    successes: 0,
                    failures: 0,
                    isStable: true)
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, actor);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        WatchtowerCombatStepResult step = result.AutomaticSteps[0];
        Assert.Equal(WatchtowerCombatStepKind.TurnAdvanced, step.Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.StableParticipant,
            step.TurnAdvanceReason);
        Assert.Empty(step.Dice);
        Assert.Equal(actorId, step.ActorCombatantId);
    }

    [Fact]
    public void AdvanceToDecision_MeleeRaider_AttacksNearestConsciousPartyTarget()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee");
        string expectedTarget = source.Party.Members
            .Select((member, index) => new
            {
                Member = member,
                Index = index,
                Participant =
                    WatchtowerCombatTestData.GetParticipant(
                        source,
                        member.PartyMemberId)
            })
            .Where(entry =>
                entry.Participant.Combatant.LifecycleState
                    == CombatantLifecycleState.Conscious)
            .OrderBy(entry => DistanceFeet(
                raider.Position,
                entry.Participant.Position))
            .ThenBy(entry => entry.Index)
            .First()
            .Member.PartyMemberId;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        WatchtowerCombatStepResult attack = Assert.Single(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.WeaponAttack);
        Assert.Equal(
            "combatant.watchtower-raider.melee",
            attack.ActorCombatantId);
        Assert.Equal(expectedTarget, attack.TargetCombatantId);
        Assert.NotNull(attack.WeaponAttack);
        Assert.Equal(encounter.Revision, source.ActiveEncounter!.Encounter.Revision);
    }

    [Fact]
    public void AdvanceToDecision_NearestUnattackableTarget_AttacksFartherLegalTarget()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");

        source = SetPosition(
            source,
            "party-member.fighter",
            new GridPosition(0, 0));
        source = SetPosition(
            source,
            "party-member.barbarian",
            new GridPosition(0, 2));
        source = SetPosition(
            source,
            "party-member.ranger",
            new GridPosition(4, 3));
        source = SetPosition(
            source,
            "combatant.watchtower-raider.melee",
            new GridPosition(2, 0));
        source = SetPosition(
            source,
            "combatant.watchtower-raider.ranged",
            new GridPosition(4, 0));

        EncounterParticipantState barbarian =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "party-member.barbarian");
        barbarian = barbarian with
        {
            Combatant = barbarian.Combatant with
            {
                Health = WatchtowerCombatTestData.CreateZeroHealth(
                    barbarian.Combatant.Health.HitPoints.MaximumHitPoints,
                    successes: 0,
                    failures: 0,
                    isStable: false)
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(
            source,
            barbarian);

        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        encounter = encounter with
        {
            Battlefield = encounter.Battlefield with
            {
                BlockedPositions =
                [
                    new GridPosition(0, 1),
                    new GridPosition(1, 0),
                    new GridPosition(1, 1)
                ]
            }
        };
        source = WatchtowerCombatTestData.ReplaceEncounter(source, encounter);
        encounter = WatchtowerCombatTestData.GetEncounter(source);

        WatchtowerCombatAttackAvailability nearestPrerequisites =
            WatchtowerCombatAttackStaging.EvaluateAvailability(
                encounter,
                "combatant.watchtower-raider.melee",
                "party-member.fighter",
                "weapon.watchtower-raider.scimitar");
        EncounterMovementResult? nearestMovement =
            WatchtowerCombatPathSearch.FindMovement(
                encounter,
                "combatant.watchtower-raider.melee",
                "party-member.fighter",
                "weapon.watchtower-raider.scimitar");
        EncounterMovementResult? fartherMovement =
            WatchtowerCombatPathSearch.FindMovement(
                encounter,
                "combatant.watchtower-raider.melee",
                "party-member.ranger",
                "weapon.watchtower-raider.scimitar");

        Assert.False(nearestPrerequisites.IsLegal);
        Assert.Null(nearestMovement);
        Assert.NotNull(fartherMovement);
        Assert.True(
            WatchtowerCombatAttackStaging.EvaluateAvailability(
                fartherMovement.State,
                "combatant.watchtower-raider.melee",
                "party-member.ranger",
                "weapon.watchtower-raider.scimitar").IsLegal);

        int cursorBeforeSelection = source.RandomValuesConsumed;
        EncounterParticipantState? selected =
            WatchtowerRaiderPolicy.SelectTarget(
                encounter,
                source.Party,
                WatchtowerCombatTestData.GetParticipant(
                    source,
                    "combatant.watchtower-raider.melee"));

        Assert.NotNull(selected);
        Assert.Equal(
            "party-member.ranger",
            selected.Combatant.CombatantId);
        Assert.Equal(
            cursorBeforeSelection,
            source.RandomValuesConsumed);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        WatchtowerCombatStepResult movement = Assert.Single(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.Movement
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.melee");
        WatchtowerCombatStepResult attack = Assert.Single(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.WeaponAttack
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.melee");
        Assert.Equal("party-member.ranger", attack.TargetCombatantId);
        Assert.True(
            WatchtowerCombatAttackStaging.EvaluateAvailability(
                movement.Movement!.State,
                "combatant.watchtower-raider.melee",
                "party-member.ranger",
                "weapon.watchtower-raider.scimitar").IsLegal);
        Assert.Empty(movement.Dice);
    }

    [Fact]
    public void AdvanceToDecision_NoAttackableTarget_MovesForBestProgressWithoutAttacking()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");

        source = SetPosition(
            source,
            "party-member.fighter",
            new GridPosition(0, 0));
        source = SetPosition(
            source,
            "party-member.barbarian",
            new GridPosition(0, 3));
        source = SetPosition(
            source,
            "party-member.ranger",
            new GridPosition(1, 3));
        source = SetPosition(
            source,
            "combatant.watchtower-raider.melee",
            new GridPosition(4, 3));
        source = SetPosition(
            source,
            "combatant.watchtower-raider.ranged",
            new GridPosition(4, 0));

        foreach (string combatantId in new[]
        {
            "party-member.barbarian",
            "party-member.ranger"
        })
        {
            EncounterParticipantState participant =
                WatchtowerCombatTestData.GetParticipant(
                    source,
                    combatantId);
            participant = participant with
            {
                Combatant = participant.Combatant with
                {
                    Health = WatchtowerCombatTestData.CreateZeroHealth(
                        participant.Combatant.Health.HitPoints.MaximumHitPoints,
                        successes: 0,
                        failures: 0,
                        isStable: false)
                }
            };
            source = WatchtowerCombatTestData.ReplaceParticipant(
                source,
                participant);
        }

        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        encounter = encounter with
        {
            Battlefield = encounter.Battlefield with
            {
                BlockedPositions =
                [
                    new GridPosition(0, 1),
                    new GridPosition(1, 0),
                    new GridPosition(1, 1)
                ]
            }
        };
        source = WatchtowerCombatTestData.ReplaceEncounter(source, encounter);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        WatchtowerCombatStepResult movement = Assert.Single(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.Movement
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.melee");
        Assert.True(
            DistanceFeet(
                movement.Movement!.EndingPosition,
                new GridPosition(0, 0))
            < DistanceFeet(
                new GridPosition(4, 3),
                new GridPosition(0, 0)));
        Assert.DoesNotContain(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.WeaponAttack
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.melee");
        WatchtowerCombatStepResult turn = Assert.Single(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.TurnAdvanced
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.melee"
                && step.TurnAdvanceReason
                    == WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction);
        Assert.Empty(movement.Dice);
        Assert.Empty(turn.Dice);
    }

    [Fact]
    public void AdvanceToDecision_RangedRaider_AttacksWithoutRepositioning()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.ranged");

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.DoesNotContain(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.Movement
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.ranged");
        Assert.Contains(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.WeaponAttack
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.ranged");
    }

    [Fact]
    public void AdvanceToDecision_RangedRaiderWithIllegalAttack_DoesNotReposition()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.ranged");
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.ranged");
        FiveEGoldBox.Core.Characters.WeaponAttack weapon = Assert.Single(
            raider.CombatProfile.WeaponAttacks);
        raider = raider with
        {
            CombatProfile = raider.CombatProfile with
            {
                WeaponAttacks =
                [
                    weapon with
                    {
                        NormalRangeFeet = 5,
                        LongRangeFeet = 5
                    }
                ]
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, raider);
        int cursorBefore = source.RandomValuesConsumed;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        WatchtowerCombatStepResult first = result.AutomaticSteps[0];
        Assert.Equal(WatchtowerCombatStepKind.TurnAdvanced, first.Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction,
            first.TurnAdvanceReason);
        Assert.Empty(first.Dice);
        Assert.DoesNotContain(
            result.AutomaticSteps,
            step => step.ActorCombatantId
                    == "combatant.watchtower-raider.ranged"
                && step.Kind is WatchtowerCombatStepKind.Movement
                    or WatchtowerCombatStepKind.WeaponAttack);
        Assert.Equal(cursorBefore, result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void AdvanceToDecision_RangedRaiderWithoutAmmunition_EndsWithoutDice()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.ranged");
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.ranged");
        FiveEGoldBox.Core.Characters.WeaponAttack weapon = Assert.Single(
            raider.CombatProfile.WeaponAttacks);
        raider = raider with
        {
            CombatProfile = raider.CombatProfile with
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
        source = WatchtowerCombatTestData.ReplaceParticipant(source, raider);
        int cursorBefore = source.RandomValuesConsumed;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        WatchtowerCombatStepResult first = result.AutomaticSteps[0];
        Assert.Equal(WatchtowerCombatStepKind.TurnAdvanced, first.Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction,
            first.TurnAdvanceReason);
        Assert.Empty(first.Dice);
        Assert.Equal(cursorBefore, result.RandomValuesConsumedAfter);
    }



    [Fact]
    public void AdvanceToDecision_RangedRaiderAdjacentToConsciousHostile_AttacksWithDisadvantage()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.ranged");
        EncounterParticipantState fighter =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "party-member.fighter") with
            {
                Position = new GridPosition(3, 2)
            };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, fighter);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.DoesNotContain(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.Movement
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.ranged");
        WatchtowerCombatStepResult attack = Assert.Single(
            result.AutomaticSteps,
            step => step.Kind == WatchtowerCombatStepKind.WeaponAttack
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.ranged");
        Assert.Equal("party-member.fighter", attack.TargetCombatantId);
        Assert.Equal(
            D20RollMode.Disadvantage,
            attack.WeaponAttack!.Attack.AttackRoll.RollMode);
        Assert.Equal(
            2,
            attack.Dice.Count(die =>
                die.Purpose == WatchtowerCombatDiePurpose.AttackRoll));
    }

    [Fact]
    public void AdvanceToDecision_RaiderWithNoConsciousTarget_EndsTurnWithoutAttackDice()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");

        foreach (string partyMemberId in source.Party.Members
            .Select(member => member.PartyMemberId))
        {
            EncounterParticipantState participant =
                WatchtowerCombatTestData.GetParticipant(
                    source,
                    partyMemberId);
            participant = participant with
            {
                Combatant = participant.Combatant with
                {
                    Health = WatchtowerCombatTestData.CreateZeroHealth(
                        participant.Combatant.Health.HitPoints.MaximumHitPoints,
                        successes: 0,
                        failures: 0,
                        isStable: false)
                }
            };
            source = WatchtowerCombatTestData.ReplaceParticipant(
                source,
                participant);
        }

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        WatchtowerCombatStepResult first = result.AutomaticSteps[0];
        Assert.Equal(WatchtowerCombatStepKind.TurnAdvanced, first.Kind);
        Assert.Equal(
            "combatant.watchtower-raider.melee",
            first.ActorCombatantId);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction,
            first.TurnAdvanceReason);
        Assert.Empty(first.Dice);
        Assert.DoesNotContain(
            result.AutomaticSteps,
            step => step.ActorCombatantId
                    == "combatant.watchtower-raider.melee"
                && step.Kind == WatchtowerCombatStepKind.WeaponAttack);
    }

    [Fact]
    public void AdvanceToDecision_MeleeRaiderOutOfReach_UsesCoreValidatedMovementThenAttacks()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee") with
            {
                Position = new GridPosition(4, 3)
            };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, raider);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        int movementIndex = result.AutomaticSteps.FindIndex(
            step => step.Kind == WatchtowerCombatStepKind.Movement
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.melee");
        int attackIndex = result.AutomaticSteps.FindIndex(
            step => step.Kind == WatchtowerCombatStepKind.WeaponAttack
                && step.ActorCombatantId
                    == "combatant.watchtower-raider.melee");

        Assert.True(movementIndex >= 0);
        Assert.True(attackIndex > movementIndex);
        Assert.Equal(
            new[]
            {
                new GridPosition(3, 2),
                new GridPosition(2, 1)
            },
            result.AutomaticSteps[movementIndex].Movement!.Path);
    }

    private static ApplicationSessionState SetPosition(
        ApplicationSessionState source,
        string combatantId,
        GridPosition position)
    {
        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(
                source,
                combatantId) with
            {
                Position = position
            };

        return WatchtowerCombatTestData.ReplaceParticipant(
            source,
            participant);
    }

    private static int DistanceFeet(
        GridPosition first,
        GridPosition second)
    {
        return Math.Max(
            Math.Abs(first.X - second.X),
            Math.Abs(first.Y - second.Y)) * 5;
    }
}

internal static class WatchtowerCombatStepListExtensions
{
    internal static int FindIndex(
        this IReadOnlyList<WatchtowerCombatStepResult> steps,
        Func<WatchtowerCombatStepResult, bool> predicate)
    {
        for (int index = 0; index < steps.Count; index++)
        {
            if (predicate(steps[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
