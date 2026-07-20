using System.Collections;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerCombatExecutionTests
{
    [Fact]
    public void Execute_Move_UsesCompletePathAndConsumesNoRandomness()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision decision = GetDecision(source);
        GridPosition[] path = FindLegalOneStepPath(source);
        int cursor = source.RandomValuesConsumed;
        EncounterState originalEncounter =
            WatchtowerCombatTestData.GetEncounter(source);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    Path = path
                });

        Assert.NotNull(result.PrimaryStep);
        Assert.Equal(
            WatchtowerCombatStepKind.Movement,
            result.PrimaryStep.Kind);
        Assert.Equal(path, result.PrimaryStep.Movement!.Path);
        Assert.Equal(path[^1], result.PrimaryStep.Movement.EndingPosition);
        Assert.Equal(cursor, result.RandomValuesConsumedAfter);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.True(
            WatchtowerCombatTestData.GetParticipant(
                result.State,
                decision.ActiveCombatantId!)
            .TurnResources.HasActionAvailable);
        Assert.Equal(1, originalEncounter.Revision);
        Assert.NotEqual(
            originalEncounter.Revision,
            result.ResultingEncounterRevision);
    }

    [Fact]
    public void Execute_Move_SupportsMultipleMovementIncrements()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision firstDecision = GetDecision(source);
        GridPosition[] firstPath = FindLegalOneStepPath(source);
        ApplicationSessionState afterFirst =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = firstDecision.EncounterRevision,
                    ActorCombatantId = firstDecision.ActiveCombatantId!,
                    Path = firstPath
                }).State;
        WatchtowerCombatDecision secondDecision = GetDecision(afterFirst);
        GridPosition[] secondPath = FindLegalOneStepPath(afterFirst);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                afterFirst,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = secondDecision.EncounterRevision,
                    ActorCombatantId = secondDecision.ActiveCombatantId!,
                    Path = secondPath
                });

        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(
                result.State,
                secondDecision.ActiveCombatantId!);

        Assert.Equal(10, participant.TurnResources.MovementSpentFeet);
        Assert.True(participant.TurnResources.HasActionAvailable);
        Assert.Equal(source.RandomValuesConsumed, result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void Execute_WeaponAttack_GeneratesAuthoritativeDiceAndSpendsAction()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatTargetOption target =
            decision.WeaponAttack!.Targets.First(
                candidate => candidate.IsAvailable);
        int cursorBefore = source.RandomValuesConsumed;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });

        WatchtowerCombatStepResult step = Assert.IsType<WatchtowerCombatStepResult>(
            result.PrimaryStep);
        EncounterWeaponAttackResult attack = Assert.IsType<EncounterWeaponAttackResult>(
            step.WeaponAttack);
        int expectedAttackDice = target.AttackRollMode == FiveEGoldBox.Core.Rules.D20RollMode.Normal
            ? 1
            : 2;
        int expectedDamageDice = attack.Attack.AttackRoll.Outcome
            == FiveEGoldBox.Core.Rules.AttackRollOutcome.Miss
                ? 0
                : attack.Attack.Damage.DamageDice!.Count;

        Assert.Equal(WatchtowerCombatStepKind.WeaponAttack, step.Kind);
        Assert.Equal(expectedAttackDice, step.Dice.Count(die =>
            die.Purpose == WatchtowerCombatDiePurpose.AttackRoll));
        Assert.Equal(expectedDamageDice, step.Dice.Count(die =>
            die.Purpose == WatchtowerCombatDiePurpose.DamageRoll));
        Assert.Equal(
            expectedAttackDice + expectedDamageDice,
            result.RandomValuesConsumedAfter - cursorBefore);
        Assert.False(
            WatchtowerCombatTestData.GetParticipant(
                result.State,
                decision.ActiveCombatantId!)
            .TurnResources.HasActionAvailable);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
    }

    [Fact]
    public void Execute_RangerAttack_ConsumesCoreAmmunitionWithoutProjectingPartyState()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerCombatTestData.CreatePlayerDecisionSession(),
                "party-member.ranger");
        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatTargetOption target =
            decision.WeaponAttack!.Targets.First(candidate => candidate.IsAvailable);
        int persistentAmmunition =
            source.Party.Members[2].Ammunition!.RemainingQuantity;
        int encounterAmmunition = Assert.Single(
            WatchtowerCombatTestData.GetParticipant(
                source,
                "party-member.ranger")
            .CombatProfile.WeaponAttacks)
            .AmmunitionQuantityAvailable!.Value;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });

        Assert.Equal(
            encounterAmmunition - 1,
            Assert.Single(WatchtowerCombatTestData.GetParticipant(
                result.State,
                "party-member.ranger")
            .CombatProfile.WeaponAttacks)
            .AmmunitionQuantityAvailable);
        Assert.Equal(
            persistentAmmunition,
            result.State.Party.Members[2].Ammunition!.RemainingQuantity);
    }

    [Fact]
    public void Execute_AdjacentRangerAttack_ConsumesTwoDisadvantageD20s()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerCombatTestData.CreatePlayerDecisionSession(),
                "party-member.ranger");
        EncounterParticipantState ranger =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "party-member.ranger") with
            {
                Position = new GridPosition(2, 2)
            };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, ranger);
        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatTargetOption target = Assert.Single(
            decision.WeaponAttack!.Targets,
            candidate => candidate.TargetCombatantId
                == "combatant.watchtower-raider.melee");

        Assert.Equal(
            FiveEGoldBox.Core.Rules.D20RollMode.Disadvantage,
            target.AttackRollMode);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });

        Assert.Equal(2, result.PrimaryStep!.Dice.Count(die =>
            die.Purpose == WatchtowerCombatDiePurpose.AttackRoll));
        Assert.Equal(
            FiveEGoldBox.Core.Rules.D20RollMode.Disadvantage,
            result.PrimaryStep.WeaponAttack!.Attack.AttackRoll.RollMode);
    }

    [Fact]
    public void Execute_WeaponAttackMiss_ConsumesAttackDieAndNoDamageDice()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession() with
            {
                RandomValuesConsumed = 3
            };
        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatTargetOption target =
            decision.WeaponAttack!.Targets.First(candidate => candidate.IsAvailable);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });

        Assert.Equal(
            FiveEGoldBox.Core.Rules.AttackRollOutcome.Miss,
            result.PrimaryStep!.WeaponAttack!.Attack.AttackRoll.Outcome);
        Assert.Single(result.PrimaryStep.Dice);
        Assert.Equal(
            WatchtowerCombatDiePurpose.AttackRoll,
            result.PrimaryStep.Dice[0].Purpose);
        Assert.Equal(1, result.PrimaryStep.Dice[0].Value);
        Assert.Equal(4, result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void Execute_WeaponCritical_ConsumesDoubledDamageDice()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession() with
            {
                RandomValuesConsumed = 35
            };
        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatTargetOption target =
            decision.WeaponAttack!.Targets.First(candidate => candidate.IsAvailable);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });

        Assert.Equal(
            FiveEGoldBox.Core.Rules.AttackRollOutcome.CriticalHit,
            result.PrimaryStep!.WeaponAttack!.Attack.AttackRoll.Outcome);
        Assert.Equal(1, result.PrimaryStep.Dice.Count(die =>
            die.Purpose == WatchtowerCombatDiePurpose.AttackRoll));
        Assert.Equal(2, result.PrimaryStep.Dice.Count(die =>
            die.Purpose == WatchtowerCombatDiePurpose.DamageRoll));
        Assert.Equal(38, result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void Execute_WeaponAttack_PreservesRemainingMovementForLaterMove()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision attackDecision = GetDecision(source);
        WatchtowerCombatTargetOption target =
            attackDecision.WeaponAttack!.Targets.First(
                candidate => candidate.IsAvailable);
        ApplicationSessionState afterAttack =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = attackDecision.EncounterRevision,
                    ActorCombatantId = attackDecision.ActiveCombatantId!,
                    WeaponId = attackDecision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                }).State;
        WatchtowerCombatDecision moveDecision = GetDecision(afterAttack);
        GridPosition[] path = FindLegalOneStepPath(afterAttack);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                afterAttack,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = moveDecision.EncounterRevision,
                    ActorCombatantId = moveDecision.ActiveCombatantId!,
                    Path = path
                });

        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(
                result.State,
                moveDecision.ActiveCombatantId!);

        Assert.False(participant.TurnResources.HasActionAvailable);
        Assert.Equal(5, participant.TurnResources.MovementSpentFeet);
    }

    [Fact]
    public void Execute_MoveAttackMoveEndTurn_SupportsSplitMovementAroundAction()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision firstDecision = GetDecision(source);

        ApplicationSessionState afterFirstMove =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = firstDecision.EncounterRevision,
                    ActorCombatantId = firstDecision.ActiveCombatantId!,
                    Path = [new GridPosition(2, 0)]
                }).State;
        WatchtowerCombatDecision attackDecision = GetDecision(afterFirstMove);
        WatchtowerCombatTargetOption target = Assert.Single(
            attackDecision.WeaponAttack!.Targets,
            candidate => candidate.TargetCombatantId
                == "combatant.watchtower-raider.melee");
        Assert.True(target.IsAvailable);

        ApplicationSessionState afterAttack =
            WatchtowerCombatRules.Execute(
                afterFirstMove,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = attackDecision.EncounterRevision,
                    ActorCombatantId = attackDecision.ActiveCombatantId!,
                    WeaponId = attackDecision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                }).State;
        WatchtowerCombatDecision secondMoveDecision = GetDecision(afterAttack);

        ApplicationSessionState afterSecondMove =
            WatchtowerCombatRules.Execute(
                afterAttack,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = secondMoveDecision.EncounterRevision,
                    ActorCombatantId = secondMoveDecision.ActiveCombatantId!,
                    Path = [new GridPosition(3, 0)]
                }).State;
        WatchtowerCombatDecision endDecision = GetDecision(afterSecondMove);
        WatchtowerCombatResolutionResult ended =
            WatchtowerCombatRules.Execute(
                afterSecondMove,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = endDecision.EncounterRevision,
                    ActorCombatantId = endDecision.ActiveCombatantId!
                });

        EncounterParticipantState fighter =
            WatchtowerCombatTestData.GetParticipant(
                afterSecondMove,
                firstDecision.ActiveCombatantId!);

        Assert.Equal(10, fighter.TurnResources.MovementSpentFeet);
        Assert.False(fighter.TurnResources.HasActionAvailable);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.PlayerEndTurn,
            ended.PrimaryStep!.TurnAdvanceReason);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            ended.ResultingDecision.State);
    }

    [Fact]
    public void Execute_MoveMoveAttackMoveEndTurn_SupportsMultipleSplitIncrements()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision decision = GetDecision(source);
        string actorId = decision.ActiveCombatantId!;

        source = WatchtowerCombatRules.Execute(
            source,
            new WatchtowerCombatMoveIntent
            {
                ExpectedEncounterRevision = decision.EncounterRevision,
                ActorCombatantId = actorId,
                Path = [new GridPosition(2, 0)]
            }).State;
        decision = GetDecision(source);
        source = WatchtowerCombatRules.Execute(
            source,
            new WatchtowerCombatMoveIntent
            {
                ExpectedEncounterRevision = decision.EncounterRevision,
                ActorCombatantId = actorId,
                Path = [new GridPosition(3, 0)]
            }).State;
        decision = GetDecision(source);
        WatchtowerCombatTargetOption target = Assert.Single(
            decision.WeaponAttack!.Targets,
            candidate => candidate.TargetCombatantId
                == "combatant.watchtower-raider.melee");
        Assert.True(target.IsAvailable);
        source = WatchtowerCombatRules.Execute(
            source,
            new WatchtowerCombatWeaponAttackIntent
            {
                ExpectedEncounterRevision = decision.EncounterRevision,
                ActorCombatantId = actorId,
                WeaponId = decision.WeaponAttack.WeaponId,
                TargetCombatantId = target.TargetCombatantId
            }).State;
        decision = GetDecision(source);
        source = WatchtowerCombatRules.Execute(
            source,
            new WatchtowerCombatMoveIntent
            {
                ExpectedEncounterRevision = decision.EncounterRevision,
                ActorCombatantId = actorId,
                Path = [new GridPosition(4, 0)]
            }).State;
        decision = GetDecision(source);
        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = actorId
                });

        EncounterParticipantState fighter =
            WatchtowerCombatTestData.GetParticipant(source, actorId);
        Assert.Equal(15, fighter.TurnResources.MovementSpentFeet);
        Assert.False(fighter.TurnResources.HasActionAvailable);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.PlayerEndTurn,
            result.PrimaryStep!.TurnAdvanceReason);
    }

    [Fact]
    public void Execute_SecondWeaponAttack_RejectsWithoutGeneratingDice()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatTargetOption target =
            decision.WeaponAttack!.Targets.First(candidate => candidate.IsAvailable);
        ApplicationSessionState afterAttack =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                }).State;
        WatchtowerCombatDecision afterDecision = GetDecision(afterAttack);
        int cursor = afterAttack.RandomValuesConsumed;
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(afterAttack);

        Assert.False(afterDecision.WeaponAttack!.IsAvailable);
        Assert.All(
            afterDecision.WeaponAttack.Targets,
            option => Assert.Equal(
                EncounterActionUnavailabilityReason.ActionUnavailable,
                option.UnavailabilityReason));
        Assert.Throws<InvalidOperationException>(() =>
            WatchtowerCombatRules.Execute(
                afterAttack,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = afterDecision.EncounterRevision,
                    ActorCombatantId = afterDecision.ActiveCombatantId!,
                    WeaponId = afterDecision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                }));

        Assert.Equal(cursor, afterAttack.RandomValuesConsumed);
        Assert.Equal(encounter, WatchtowerCombatTestData.GetEncounter(afterAttack));
    }

    [Fact]
    public void Execute_IllegalMovement_RejectsBeforeRandomnessAndPreservesNestedState()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision decision = GetDecision(source);
        RejectedOperationSnapshot snapshot = CaptureRejectedOperation(source);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    Path = [new GridPosition(-1, -1)]
                }));

        AssertRejectedOperationPreserved(snapshot, source);
    }

    [Fact]
    public void Execute_ZeroAmmunitionAttack_RejectsBeforeDiceAndLeavesTurnAvailable()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerCombatTestData.CreatePlayerDecisionSession(),
                "party-member.ranger");
        EncounterParticipantState ranger =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "party-member.ranger");
        WeaponAttack weapon = Assert.Single(
            ranger.CombatProfile.WeaponAttacks);
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
        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatTargetOption target = decision.WeaponAttack!.Targets[0];
        RejectedOperationSnapshot snapshot = CaptureRejectedOperation(source);

        Assert.Throws<InvalidOperationException>(() =>
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                }));

        AssertRejectedOperationPreserved(snapshot, source);
        WatchtowerCombatDecision afterRejection = GetDecision(source);
        Assert.False(afterRejection.WeaponAttack!.IsAvailable);
        Assert.True(afterRejection.Movement!.IsAvailable);
        Assert.True(afterRejection.EndTurn!.IsAvailable);
        EncounterParticipantState unchangedRanger =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "party-member.ranger");
        Assert.Equal(
            0,
            Assert.Single(unchangedRanger.CombatProfile.WeaponAttacks)
                .AmmunitionQuantityAvailable);
        Assert.True(unchangedRanger.TurnResources.HasActionAvailable);
        Assert.Equal(0, unchangedRanger.TurnResources.MovementSpentFeet);
    }

    [Fact]
    public void Execute_NormalIntentWhileDeathSavePending_RejectsWithoutDiceOrTurnResourceChanges()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreateDyingActiveSession(
                randomValuesConsumed: 10,
                successes: 1,
                failures: 0);
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        RejectedOperationSnapshot snapshot = CaptureRejectedOperation(source);

        Assert.Throws<InvalidOperationException>(() =>
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = encounter.Revision,
                    ActorCombatantId = encounter.ActiveCombatantId,
                    Path = [new GridPosition(2, 0)]
                }));

        AssertRejectedOperationPreserved(snapshot, source);
        EncounterState unchanged = WatchtowerCombatTestData.GetEncounter(source);
        Assert.Equal(
            encounter.ActiveCombatantId,
            unchanged.PendingDeathSavingThrowCombatantId);
        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(
                source,
                encounter.ActiveCombatantId);
        Assert.True(participant.TurnResources.HasActionAvailable);
        Assert.Equal(0, participant.TurnResources.MovementSpentFeet);
        Assert.Equal(CombatantLifecycleState.Dying, participant.Combatant.LifecycleState);
    }

    [Fact]
    public void Execute_FinalCoreResolutionFailure_DoesNotCommitProposedAttackOrDamageDice()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession() with
            {
                RandomValuesConsumed = 35
            };
        WatchtowerCombatDecision decision = GetDecision(source);
        WatchtowerCombatTargetOption target =
            decision.WeaponAttack!.Targets.First(
                candidate => candidate.IsAvailable);
        EncounterParticipantState targetParticipant =
            WatchtowerCombatTestData.GetParticipant(
                source,
                target.TargetCombatantId);
        targetParticipant = targetParticipant with
        {
            CombatProfile = targetParticipant.CombatProfile with
            {
                DamageResponses = new ThrowingDamageResponseList()
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(
            source,
            targetParticipant);
        decision = GetDecision(source);
        target = decision.WeaponAttack!.Targets.First(
            candidate => candidate.IsAvailable);
        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(source);

        EncounterWeaponAttackPrerequisiteEvaluation prerequisites =
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                encounter,
                decision.ActiveCombatantId!,
                target.TargetCombatantId,
                decision.WeaponAttack.WeaponId);
        Assert.True(prerequisites.IsLegal);

        ApplicationRandomRoll attackRoll =
            ApplicationRandomSequence.GenerateDie(
                source.RandomSeed,
                source.RandomValuesConsumed,
                sides: 20);
        Assert.Equal(20, attackRoll.Value);
        EncounterWeaponAttackEvaluation evaluation =
            EncounterWeaponAttackRules.Evaluate(
                encounter,
                new EncounterWeaponAttackEvaluationCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    TargetCombatantId = target.TargetCombatantId,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    FirstAttackRoll = attackRoll.Value,
                    SecondAttackRoll = null
                });
        DamageDice requiredDamage = Assert.IsType<DamageDice>(
            evaluation.RequiredDamageDice);
        Assert.Equal(
            AttackRollOutcome.CriticalHit,
            evaluation.AttackRoll.Outcome);

        List<int> proposedDamageValues = [];
        int proposedCursor = attackRoll.UpdatedValuesConsumed;

        for (int index = 0; index < requiredDamage.Count; index++)
        {
            ApplicationRandomRoll damageRoll =
                ApplicationRandomSequence.GenerateDie(
                    source.RandomSeed,
                    proposedCursor,
                    sides: (int)requiredDamage.Die);

            proposedDamageValues.Add(damageRoll.Value);
            proposedCursor = damageRoll.UpdatedValuesConsumed;
        }

        Assert.Equal(requiredDamage.Count, proposedDamageValues.Count);
        Assert.All(
            proposedDamageValues,
            value => Assert.InRange(
                value,
                1,
                (int)requiredDamage.Die));
        Assert.True(proposedCursor > source.RandomValuesConsumed);

        RejectedOperationSnapshot snapshot = CaptureRejectedOperation(source);
        WatchtowerCombatResolutionResult? failedResult = null;

        Assert.Throws<ControlledDamageResponseEnumerationException>(() =>
            failedResult = WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                }));

        Assert.Null(failedResult);
        AssertRejectedOperationPreserved(snapshot, source);

        WatchtowerCombatResolutionResult reusable =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            reusable.ResultingDecision.State);
        Assert.Null(reusable.PrimaryStep);
        Assert.Empty(reusable.AutomaticSteps);
        Assert.Equal(
            snapshot.RandomValuesConsumed,
            reusable.RandomValuesConsumedBefore);
        Assert.Equal(
            snapshot.RandomValuesConsumed,
            reusable.RandomValuesConsumedAfter);
        AssertRejectedOperationPreserved(snapshot, reusable.State);
    }

    [Fact]
    public void Execute_EndTurn_ConsumesNoRandomnessForPlayerStepAndNormalizes()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision decision = GetDecision(source);
        int cursorBefore = source.RandomValuesConsumed;

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!
                });

        Assert.Equal(
            WatchtowerCombatStepKind.TurnAdvanced,
            result.PrimaryStep!.Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.PlayerEndTurn,
            result.PrimaryStep.TurnAdvanceReason);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.Empty(result.PrimaryStep.Dice);
        int automaticDice = result.AutomaticSteps.Sum(
            step => step.Dice.Count);
        Assert.Equal(
            automaticDice,
            result.RandomValuesConsumedAfter - cursorBefore);
        Assert.Empty(result.AutomaticSteps);
        Assert.Equal(cursorBefore, result.RandomValuesConsumedAfter);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Execute_WithStaleRevisionOrActor_RejectsBeforeRandomness(
        bool staleRevision,
        bool staleActor)
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision decision = GetDecision(source);
        int cursor = source.RandomValuesConsumed;
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);

        Assert.Throws<InvalidOperationException>(() =>
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = staleRevision
                        ? decision.EncounterRevision + 1
                        : decision.EncounterRevision,
                    ActorCombatantId = staleActor
                        ? "combatant.other"
                        : decision.ActiveCombatantId!
                }));

        Assert.Equal(cursor, source.RandomValuesConsumed);
        Assert.Equal(encounter, WatchtowerCombatTestData.GetEncounter(source));
    }

    [Fact]
    public void Execute_UnavailableAttack_RejectsWithoutCursorOrStateCommitment()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision decision = GetDecision(source);
        int cursor = source.RandomValuesConsumed;
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);

        Assert.Throws<InvalidOperationException>(() =>
            WatchtowerCombatRules.Execute(
                source,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = decision.WeaponAttack!.WeaponId,
                    TargetCombatantId = decision.ActiveCombatantId!
                }));

        Assert.Equal(cursor, source.RandomValuesConsumed);
        Assert.Equal(encounter, WatchtowerCombatTestData.GetEncounter(source));
    }

    private static RejectedOperationSnapshot CaptureRejectedOperation(
        ApplicationSessionState source)
    {
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        RejectedParticipantSnapshot[] participants = encounter.Participants
            .Select(participant => new RejectedParticipantSnapshot(
                participant.Combatant.CombatantId,
                participant.Position,
                participant.TurnResources.MovementSpentFeet,
                participant.TurnResources.HasActionAvailable,
                participant.CombatProfile.WeaponAttacks
                    .Select(attack => new RejectedWeaponSnapshot(
                        attack.WeaponId,
                        attack.AmmunitionQuantityAvailable))
                    .ToArray(),
                participant.Combatant.Health.HitPoints.MaximumHitPoints,
                participant.Combatant.Health.HitPoints.CurrentHitPoints,
                participant.Combatant.Health.HitPoints.TemporaryHitPoints,
                participant.Combatant.LifecycleState,
                participant.Combatant.Health.DeathSavingThrows.SuccessCount,
                participant.Combatant.Health.DeathSavingThrows.FailureCount))
            .ToArray();

        return new RejectedOperationSnapshot(
            source.CurrentMode,
            source.Scenario.Progress,
            source.ActiveEncounter!.ReturnContext,
            source.Party.PartyId,
            source.Party.Members.ToArray(),
            source.RandomSeed,
            encounter.Revision,
            encounter.ActiveCombatantId,
            encounter.PendingDeathSavingThrowCombatantId,
            encounter.LifecycleState,
            encounter.WinningSideId,
            participants,
            source.RandomValuesConsumed);
    }

    private static void AssertRejectedOperationPreserved(
        RejectedOperationSnapshot expected,
        ApplicationSessionState actual)
    {
        Assert.Equal(expected.CurrentMode, actual.CurrentMode);
        Assert.Equal(expected.ScenarioProgress, actual.Scenario.Progress);
        Assert.Equal(expected.ReturnContext, actual.ActiveEncounter!.ReturnContext);
        Assert.Equal(expected.PartyId, actual.Party.PartyId);
        Assert.Equal(expected.PartyMembers.Length, actual.Party.Members.Count);
        Assert.Equal(expected.RandomSeed, actual.RandomSeed);

        for (int index = 0; index < expected.PartyMembers.Length; index++)
        {
            Assert.Equal(
                expected.PartyMembers[index],
                actual.Party.Members[index]);
        }

        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(actual);
        Assert.Equal(expected.EncounterRevision, encounter.Revision);
        Assert.Equal(expected.ActiveCombatantId, encounter.ActiveCombatantId);
        Assert.Equal(
            expected.PendingDeathSavingThrowCombatantId,
            encounter.PendingDeathSavingThrowCombatantId);
        Assert.Equal(expected.EncounterLifecycleState, encounter.LifecycleState);
        Assert.Equal(expected.WinningSideId, encounter.WinningSideId);
        Assert.Equal(expected.Participants.Length, encounter.Participants.Count);

        for (int index = 0; index < expected.Participants.Length; index++)
        {
            RejectedParticipantSnapshot expectedParticipant =
                expected.Participants[index];
            EncounterParticipantState actualParticipant =
                encounter.Participants[index];

            Assert.Equal(
                expectedParticipant.CombatantId,
                actualParticipant.Combatant.CombatantId);
            Assert.Equal(expectedParticipant.Position, actualParticipant.Position);
            Assert.Equal(
                expectedParticipant.MovementSpentFeet,
                actualParticipant.TurnResources.MovementSpentFeet);
            Assert.Equal(
                expectedParticipant.HasActionAvailable,
                actualParticipant.TurnResources.HasActionAvailable);
            Assert.Equal(
                expectedParticipant.MaximumHitPoints,
                actualParticipant.Combatant.Health.HitPoints.MaximumHitPoints);
            Assert.Equal(
                expectedParticipant.CurrentHitPoints,
                actualParticipant.Combatant.Health.HitPoints.CurrentHitPoints);
            Assert.Equal(
                expectedParticipant.TemporaryHitPoints,
                actualParticipant.Combatant.Health.HitPoints.TemporaryHitPoints);
            Assert.Equal(
                expectedParticipant.LifecycleState,
                actualParticipant.Combatant.LifecycleState);
            Assert.Equal(
                expectedParticipant.DeathSaveSuccesses,
                actualParticipant.Combatant.Health.DeathSavingThrows.SuccessCount);
            Assert.Equal(
                expectedParticipant.DeathSaveFailures,
                actualParticipant.Combatant.Health.DeathSavingThrows.FailureCount);
            Assert.Equal(
                expectedParticipant.Weapons.Length,
                actualParticipant.CombatProfile.WeaponAttacks.Count);

            for (int weaponIndex = 0;
                weaponIndex < expectedParticipant.Weapons.Length;
                weaponIndex++)
            {
                Assert.Equal(
                    expectedParticipant.Weapons[weaponIndex].WeaponId,
                    actualParticipant.CombatProfile
                        .WeaponAttacks[weaponIndex]
                        .WeaponId);
                Assert.Equal(
                    expectedParticipant.Weapons[weaponIndex]
                        .AmmunitionQuantityAvailable,
                    actualParticipant.CombatProfile
                        .WeaponAttacks[weaponIndex]
                        .AmmunitionQuantityAvailable);
            }
        }

        Assert.Equal(expected.RandomValuesConsumed, actual.RandomValuesConsumed);
    }

    private sealed class ThrowingDamageResponseList
        : IReadOnlyList<CharacterDamageResponse>
    {
        public int Count => 0;

        public CharacterDamageResponse this[int index] =>
            throw new ArgumentOutOfRangeException(nameof(index));

        public IEnumerator<CharacterDamageResponse> GetEnumerator()
        {
            throw new ControlledDamageResponseEnumerationException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class ControlledDamageResponseEnumerationException
        : Exception
    {
    }

    private sealed record RejectedOperationSnapshot(
        ApplicationMode CurrentMode,
        FiveEGoldBox.Application.Scenarios.WatchtowerScenarioProgress ScenarioProgress,
        FiveEGoldBox.Application.Exploration.ExplorationState ReturnContext,
        string PartyId,
        FiveEGoldBox.Application.Parties.PartyMemberState[] PartyMembers,
        int RandomSeed,
        long EncounterRevision,
        string ActiveCombatantId,
        string? PendingDeathSavingThrowCombatantId,
        EncounterLifecycleState EncounterLifecycleState,
        string? WinningSideId,
        RejectedParticipantSnapshot[] Participants,
        int RandomValuesConsumed);

    private sealed record RejectedParticipantSnapshot(
        string CombatantId,
        GridPosition Position,
        int MovementSpentFeet,
        bool HasActionAvailable,
        RejectedWeaponSnapshot[] Weapons,
        int MaximumHitPoints,
        int CurrentHitPoints,
        int TemporaryHitPoints,
        CombatantLifecycleState LifecycleState,
        int DeathSaveSuccesses,
        int DeathSaveFailures);

    private sealed record RejectedWeaponSnapshot(
        string WeaponId,
        int? AmmunitionQuantityAvailable);

    private static WatchtowerCombatDecision GetDecision(
        ApplicationSessionState state)
    {
        return WatchtowerCombatRules.AdvanceToDecision(state)
            .ResultingDecision;
    }

    private static GridPosition[] FindLegalOneStepPath(
        ApplicationSessionState state)
    {
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(state);
        EncounterParticipantState actor =
            WatchtowerCombatTestData.GetParticipant(
                state,
                encounter.ActiveCombatantId);
        GridPosition[] offsets =
        [
            new(0, -1),
            new(-1, 0),
            new(1, 0),
            new(0, 1),
            new(-1, -1),
            new(1, -1),
            new(-1, 1),
            new(1, 1)
        ];

        foreach (GridPosition offset in offsets)
        {
            GridPosition candidate = new(
                actor.Position.X + offset.X,
                actor.Position.Y + offset.Y);

            try
            {
                _ = EncounterMovementRules.Resolve(
                    encounter,
                    new EncounterMovementCommand
                    {
                        ExpectedRevision = encounter.Revision,
                        ActorCombatantId = actor.Combatant.CombatantId,
                        Path = [candidate]
                    });

                return [candidate];
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException(
            "The test player has no legal one-step movement path.");
    }
}
