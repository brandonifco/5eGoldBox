using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CombatTurnResourceRulesTests
{
    [Fact]
    public void StartTurn_WithMovementSpeed_ReturnsFreshTurnResources()
    {
        CombatTurnResources result = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        Assert.True(result.HasActionAvailable);
        Assert.True(result.HasBonusActionAvailable);
        Assert.True(result.HasReactionAvailable);
        Assert.Equal(30, result.MovementSpeedFeet);
        Assert.Equal(0, result.MovementSpentFeet);
        Assert.Equal(30, result.MovementRemainingFeet);
    }

    [Fact]
    public void StartTurn_WithZeroMovementSpeed_ReturnsNoMovementRemaining()
    {
        CombatTurnResources result = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 0);

        Assert.Equal(0, result.MovementSpeedFeet);
        Assert.Equal(0, result.MovementSpentFeet);
        Assert.Equal(0, result.MovementRemainingFeet);
    }

    [Fact]
    public void StartTurn_WithNegativeMovementSpeed_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatTurnResourceRules.StartTurn(
                movementSpeedFeet: -1));
    }

    [Fact]
    public void SpendAction_WhenActionIsAvailable_MarksActionSpent()
    {
        CombatTurnResources resources = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        CombatTurnResources result = CombatTurnResourceRules.SpendAction(resources);

        Assert.False(result.HasActionAvailable);
        Assert.True(result.HasBonusActionAvailable);
        Assert.True(result.HasReactionAvailable);
        Assert.Equal(30, result.MovementRemainingFeet);
    }

    [Fact]
    public void SpendAction_WhenActionIsAlreadySpent_Throws()
    {
        CombatTurnResources resources = CombatTurnResourceRules
            .StartTurn(movementSpeedFeet: 30) with
        {
            HasActionAvailable = false
        };

        Assert.Throws<InvalidOperationException>(() =>
            CombatTurnResourceRules.SpendAction(resources));
    }

    [Fact]
    public void SpendBonusAction_WhenBonusActionIsAvailable_MarksBonusActionSpent()
    {
        CombatTurnResources resources = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        CombatTurnResources result = CombatTurnResourceRules.SpendBonusAction(resources);

        Assert.True(result.HasActionAvailable);
        Assert.False(result.HasBonusActionAvailable);
        Assert.True(result.HasReactionAvailable);
        Assert.Equal(30, result.MovementRemainingFeet);
    }

    [Fact]
    public void SpendBonusAction_WhenBonusActionIsAlreadySpent_Throws()
    {
        CombatTurnResources resources = CombatTurnResourceRules
            .StartTurn(movementSpeedFeet: 30) with
        {
            HasBonusActionAvailable = false
        };

        Assert.Throws<InvalidOperationException>(() =>
            CombatTurnResourceRules.SpendBonusAction(resources));
    }

    [Fact]
    public void SpendReaction_WhenReactionIsAvailable_MarksReactionSpent()
    {
        CombatTurnResources resources = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        CombatTurnResources result = CombatTurnResourceRules.SpendReaction(resources);

        Assert.True(result.HasActionAvailable);
        Assert.True(result.HasBonusActionAvailable);
        Assert.False(result.HasReactionAvailable);
        Assert.Equal(30, result.MovementRemainingFeet);
    }

    [Fact]
    public void SpendReaction_WhenReactionIsAlreadySpent_Throws()
    {
        CombatTurnResources resources = CombatTurnResourceRules
            .StartTurn(movementSpeedFeet: 30) with
        {
            HasReactionAvailable = false
        };

        Assert.Throws<InvalidOperationException>(() =>
            CombatTurnResourceRules.SpendReaction(resources));
    }

    [Fact]
    public void SpendMovement_WhenMovementIsAvailable_IncreasesSpentMovement()
    {
        CombatTurnResources resources = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        CombatTurnResources result = CombatTurnResourceRules.SpendMovement(
            resources,
            movementFeet: 10);

        Assert.Equal(30, result.MovementSpeedFeet);
        Assert.Equal(10, result.MovementSpentFeet);
        Assert.Equal(20, result.MovementRemainingFeet);
    }

    [Fact]
    public void SpendMovement_WhenCalledMultipleTimes_AccumulatesSpentMovement()
    {
        CombatTurnResources resources = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        CombatTurnResources afterFirstMove = CombatTurnResourceRules.SpendMovement(
            resources,
            movementFeet: 10);

        CombatTurnResources result = CombatTurnResourceRules.SpendMovement(
            afterFirstMove,
            movementFeet: 15);

        Assert.Equal(25, result.MovementSpentFeet);
        Assert.Equal(5, result.MovementRemainingFeet);
    }

    [Fact]
    public void SpendMovement_WhenMovementEqualsRemainingMovement_LeavesZeroMovementRemaining()
    {
        CombatTurnResources resources = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        CombatTurnResources result = CombatTurnResourceRules.SpendMovement(
            resources,
            movementFeet: 30);

        Assert.Equal(30, result.MovementSpentFeet);
        Assert.Equal(0, result.MovementRemainingFeet);
    }

    [Fact]
    public void SpendMovement_WithZeroMovement_Throws()
    {
        CombatTurnResources resources = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatTurnResourceRules.SpendMovement(
                resources,
                movementFeet: 0));
    }

    [Fact]
    public void SpendMovement_WithNegativeMovement_Throws()
    {
        CombatTurnResources resources = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatTurnResourceRules.SpendMovement(
                resources,
                movementFeet: -5));
    }

    [Fact]
    public void SpendMovement_WhenMovementExceedsRemainingMovement_Throws()
    {
        CombatTurnResources resources = CombatTurnResourceRules.StartTurn(
            movementSpeedFeet: 30);

        Assert.Throws<InvalidOperationException>(() =>
            CombatTurnResourceRules.SpendMovement(
                resources,
                movementFeet: 35));
    }

    [Fact]
    public void SpendMovement_WithNegativeMovementSpeed_Throws()
    {
        CombatTurnResources resources = new()
        {
            HasActionAvailable = true,
            HasBonusActionAvailable = true,
            HasReactionAvailable = true,
            MovementSpeedFeet = -1,
            MovementSpentFeet = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatTurnResourceRules.SpendMovement(
                resources,
                movementFeet: 5));
    }

    [Fact]
    public void SpendMovement_WithNegativeMovementSpent_Throws()
    {
        CombatTurnResources resources = new()
        {
            HasActionAvailable = true,
            HasBonusActionAvailable = true,
            HasReactionAvailable = true,
            MovementSpeedFeet = 30,
            MovementSpentFeet = -1
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatTurnResourceRules.SpendMovement(
                resources,
                movementFeet: 5));
    }

    [Fact]
    public void SpendMovement_WithMovementSpentGreaterThanMovementSpeed_Throws()
    {
        CombatTurnResources resources = new()
        {
            HasActionAvailable = true,
            HasBonusActionAvailable = true,
            HasReactionAvailable = true,
            MovementSpeedFeet = 30,
            MovementSpentFeet = 35
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CombatTurnResourceRules.SpendMovement(
                resources,
                movementFeet: 5));
    }
}
