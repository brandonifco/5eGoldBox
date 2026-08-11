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
        // The furthest reachable destination, not the first: the ambush
        // starts the party in contact, so the nearest options are single
        // steps that stay inside a raider's reach and provoke nothing,
        // while this one leaves it and draws an opportunity attack. Picking
        // by index made this test quietly depend on enumeration order.
        CombatMovementDestinationOption destination =
            Assert.IsType<CombatMovementOption>(decision.Movement)
                .DestinationOptions
                .OrderByDescending(option => option.Path.Count)
                .First();

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

        // Movement itself still draws no dice -- primary.Dice above is
        // empty. What this move does draw is the opportunity attack it
        // provoked by leaving a raider's reach, reported as its own step
        // ahead of any automatic processing.
        CombatStepResult reaction = result.AutomaticSteps[0];

        Assert.Equal(CombatStepKind.WeaponAttack, reaction.Kind);
        Assert.True(
            Assert.IsType<CombatWeaponAttackStepDetail>(reaction.WeaponAttack)
                .IsOpportunityAttack);
        Assert.True(
            result.RandomValuesConsumedAfter
                > result.RandomValuesConsumedBefore);
    }

    /// The two features only make sense together, and only an end-to-end
    /// test can show it: the same path that draws a free attack above draws
    /// nothing at all once the mover has disengaged. Asserted on the dice
    /// cursor as well as the step list, because "no reaction step" would
    /// also be true if the attack had silently failed to resolve.
    [Fact]
    public void Execute_MoveAfterDisengaging_ProvokesNothing()
    {
        CombatResolutionResult opening = CombatOperations.AdvanceToDecision(
            WatchtowerSignalTestData.CreateEncounterSession());
        CombatDecision decision = opening.ResultingDecision;
        string actorId = Assert.IsType<string>(decision.ActiveCombatantId);
        CombatMovementDestinationOption destination =
            Assert.IsType<CombatMovementOption>(decision.Movement)
                .DestinationOptions
                .OrderByDescending(option => option.Path.Count)
                .First();

        Assert.True(
            Assert.IsType<CombatDisengageOption>(decision.Disengage)
                .IsAvailable);

        CombatResolutionResult disengaged = CombatOperations.Execute(
            opening.State,
            new CombatDisengageIntent
            {
                ExpectedEncounterRevision = decision.EncounterRevision,
                ActorCombatantId = actorId
            });

        Assert.Equal(
            CombatStepKind.Disengage,
            Assert.IsType<CombatStepResult>(disengaged.PrimaryStep).Kind);

        CombatResolutionResult moved = CombatOperations.Execute(
            disengaged.State,
            new CombatMoveIntent
            {
                ExpectedEncounterRevision =
                    disengaged.ResultingDecision.EncounterRevision,
                ActorCombatantId = actorId,
                Path = destination.Path
            });

        Assert.DoesNotContain(
            moved.AutomaticSteps,
            step => step.Kind == CombatStepKind.WeaponAttack);
        Assert.Equal(
            moved.RandomValuesConsumedBefore,
            moved.RandomValuesConsumedAfter);
    }

    /// Disengage costs the Action, so it has to stop being on offer once
    /// the action is gone — a button the write path would then refuse is
    /// worse than no button.
    [Fact]
    public void Execute_Disengage_IsNoLongerOfferedAfterwards()
    {
        CombatResolutionResult opening = CombatOperations.AdvanceToDecision(
            WatchtowerSignalTestData.CreateEncounterSession());
        CombatDecision decision = opening.ResultingDecision;

        CombatResolutionResult disengaged = CombatOperations.Execute(
            opening.State,
            new CombatDisengageIntent
            {
                ExpectedEncounterRevision = decision.EncounterRevision,
                ActorCombatantId = Assert.IsType<string>(
                    decision.ActiveCombatantId)
            });

        Assert.False(
            Assert.IsType<CombatDisengageOption>(
                disengaged.ResultingDecision.Disengage).IsAvailable);
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
