using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Tests;

/// Tests for the scenario-agnostic write facade. The underlying pipeline is
/// covered elsewhere; these pin the contract callers actually bind to.
public sealed class CombatOperationsWriteTests
{
    [Fact]
    public void AdvanceToDecision_ReachesPlayerDecisionWithoutASubmittedIntent()
    {
        CombatResolutionResult result = CombatOperations.AdvanceToDecision(
            WatchtowerSignalTestData.CreateEncounterSession());

        Assert.Null(result.SubmittedIntent);
        Assert.Null(result.PrimaryStep);
        Assert.Equal(
            CombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.NotNull(result.ResultingDecision.Movement);
        Assert.NotEmpty(result.ResultingDecision.WeaponAttacks);
    }

    [Fact]
    public void Execute_Move_ReportsAMovementStepAndEchoesTheIntent()
    {
        CombatResolutionResult opening = CombatOperations.AdvanceToDecision(
            WatchtowerSignalTestData.CreateEncounterSession());
        CombatDecision decision = opening.ResultingDecision;
        CombatMovementDestinationOption destination =
            Assert.IsType<CombatMovementOption>(decision.Movement)
                .DestinationOptions[0];

        CombatResolutionResult result = CombatOperations.Execute(
            opening.State,
            new CombatMoveIntent
            {
                ExpectedEncounterRevision = decision.EncounterRevision,
                ActorCombatantId = Assert.IsType<string>(
                    decision.ActiveCombatantId),
                Path = destination.Path
            });

        CombatStepResult primary =
            Assert.IsType<CombatStepResult>(result.PrimaryStep);
        Assert.Equal(CombatStepKind.Movement, primary.Kind);
        Assert.Empty(primary.Dice);
        Assert.Equal(
            destination.Destination,
            Assert.IsType<CombatMovementStepDetail>(primary.Movement)
                .EndingPosition);

        CombatIntentReceipt receipt =
            Assert.IsType<CombatIntentReceipt>(result.SubmittedIntent);
        Assert.Equal(CombatIntentKind.Move, receipt.Kind);
        Assert.Equal(destination.Path, receipt.Path);

        // Movement draws no dice.
        Assert.Equal(
            result.RandomValuesConsumedBefore,
            result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void Execute_WeaponAttack_ProjectsRollDetailWithoutLeakingCoreResults()
    {
        CombatResolutionResult opening = CombatOperations.AdvanceToDecision(
            WatchtowerSignalTestData.CreateEncounterSession());
        CombatDecision decision = opening.ResultingDecision;
        CombatWeaponAttackOption weapon = decision.WeaponAttacks[0];
        CombatTargetOption target = Assert.Single(
            weapon.Targets,
            candidate => candidate.IsAvailable);

        CombatResolutionResult result = CombatOperations.Execute(
            opening.State,
            new CombatWeaponAttackIntent
            {
                ExpectedEncounterRevision = decision.EncounterRevision,
                ActorCombatantId = Assert.IsType<string>(
                    decision.ActiveCombatantId),
                WeaponId = weapon.WeaponId,
                TargetCombatantId = target.TargetCombatantId
            });

        CombatStepResult primary =
            Assert.IsType<CombatStepResult>(result.PrimaryStep);
        CombatWeaponAttackStepDetail attack =
            Assert.IsType<CombatWeaponAttackStepDetail>(primary.WeaponAttack);

        Assert.Equal(weapon.WeaponId, attack.WeaponId);
        Assert.InRange(attack.NaturalRoll, 1, 20);
        Assert.Equal(attack.NaturalRoll + attack.AttackBonus, attack.AttackTotal);
        Assert.NotEmpty(primary.Dice);
        Assert.Equal(
            result.RandomValuesConsumedBefore + primary.Dice.Count,
            result.RandomValuesConsumedAfter);
    }

    /// The pipeline records a raider finishing its turn; the facade reports it
    /// under the scenario-neutral name.
    [Fact]
    public void EnemyTurns_AreReportedAsEnemyTurnCompleted()
    {
        ApplicationSessionState state =
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");

        CombatResolutionResult result =
            CombatOperations.AdvanceToDecision(state);

        Assert.Contains(
            result.AutomaticSteps,
            step => step.TurnAdvanceReason
                == CombatTurnAdvanceReason.EnemyTurnCompleted);
        Assert.DoesNotContain(
            result.AutomaticSteps,
            step => step.Kind == CombatStepKind.TurnAdvanced
                && step.TurnAdvanceReason is null);
    }
}
