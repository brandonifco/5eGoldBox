using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterLineOfSightRulesTests
{
    [Fact]
    public void Evaluate_WithClearHorizontalLine_ReturnsLineOfSight()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1));

        Assert.True(result.HasLineOfSight);
        Assert.Null(result.BlockingPosition);
        Assert.Equal(
            new GridPosition(1, 1),
            result.SourcePosition);
        Assert.Equal(
            new GridPosition(5, 1),
            result.TargetPosition);
        Assert.Equal(
            [
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                new GridPosition(4, 1)
            ],
            result.IntermediatePositions);
    }

    [Fact]
    public void Evaluate_WithClearVerticalLine_ReturnsLineOfSight()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(3, 1),
                new GridPosition(3, 5));

        Assert.True(result.HasLineOfSight);
        Assert.Null(result.BlockingPosition);
        Assert.Equal(
            [
                new GridPosition(3, 2),
                new GridPosition(3, 3),
                new GridPosition(3, 4)
            ],
            result.IntermediatePositions);
    }

    [Fact]
    public void Evaluate_WithClearDiagonalLine_UsesOnlyEnteredDiagonalSquares()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(4, 4));

        Assert.True(result.HasLineOfSight);
        Assert.Equal(
            [
                new GridPosition(2, 2),
                new GridPosition(3, 3)
            ],
            result.IntermediatePositions);
    }

    [Fact]
    public void Evaluate_WhenLineOnlyTouchesBlockedSquareCorners_DoesNotBlockSight()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    new GridPosition(2, 1),
                    new GridPosition(1, 2),
                    new GridPosition(3, 2),
                    new GridPosition(2, 3)
                ]);

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(3, 3));

        Assert.True(result.HasLineOfSight);
        Assert.Null(result.BlockingPosition);
        Assert.Equal(
            [
                new GridPosition(2, 2)
            ],
            result.IntermediatePositions);
    }

    [Fact]
    public void Evaluate_WhenIntermediateSquareIsBlocked_ReturnsNoLineOfSight()
    {
        GridPosition blockedPosition =
            new(3, 1);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    blockedPosition
                ]);

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1));

        Assert.False(result.HasLineOfSight);
        Assert.Equal(
            blockedPosition,
            result.BlockingPosition);
        Assert.Equal(
            [
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                new GridPosition(4, 1)
            ],
            result.IntermediatePositions);
    }

    [Fact]
    public void Evaluate_WithMultipleBlockedSquares_ReturnsFirstBlockerFromSource()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    new GridPosition(4, 1),
                    new GridPosition(2, 1)
                ]);

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1));

        Assert.False(result.HasLineOfSight);
        Assert.Equal(
            new GridPosition(2, 1),
            result.BlockingPosition);
    }

    [Fact]
    public void Evaluate_WithNonDiagonalSlope_ReturnsDeterministicIntermediatePositions()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 3));

        Assert.True(result.HasLineOfSight);
        Assert.Equal(
            [
                new GridPosition(2, 1),
                new GridPosition(2, 2),
                new GridPosition(3, 2),
                new GridPosition(4, 2),
                new GridPosition(4, 3)
            ],
            result.IntermediatePositions);
    }

    [Fact]
    public void Evaluate_InReverseDirection_ReturnsReversedIntermediatePositions()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        EncounterLineOfSightResult forward =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 3));

        EncounterLineOfSightResult reverse =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(5, 3),
                new GridPosition(1, 1));

        Assert.Equal(
            forward.IntermediatePositions
                .Reverse()
                .ToArray(),
            reverse.IntermediatePositions);
    }

    [Fact]
    public void Evaluate_WithAdjacentPositions_ReturnsNoIntermediatePositions()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(2, 1));

        Assert.True(result.HasLineOfSight);
        Assert.Null(result.BlockingPosition);
        Assert.Empty(result.IntermediatePositions);
    }

    [Fact]
    public void Evaluate_WithSameSourceAndTarget_ReturnsLineOfSight()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        GridPosition position =
            new(3, 3);

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                position,
                position);

        Assert.True(result.HasLineOfSight);
        Assert.Null(result.BlockingPosition);
        Assert.Empty(result.IntermediatePositions);
    }

    [Fact]
    public void Evaluate_ProtectsIntermediatePositionCollection()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        EncounterLineOfSightResult result =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(4, 1));

        IList<GridPosition> positions =
            Assert.IsAssignableFrom<
                IList<GridPosition>>(
                result.IntermediatePositions);

        Assert.Throws<NotSupportedException>(() =>
            positions[0] =
                new GridPosition(9, 9));
    }

    [Fact]
    public void Evaluate_WhenSourcePositionIsBlocked_Throws()
    {
        GridPosition sourcePosition =
            new(1, 1);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    sourcePosition
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                sourcePosition,
                new GridPosition(5, 1)));
    }

    [Fact]
    public void Evaluate_WhenTargetPositionIsBlocked_Throws()
    {
        GridPosition targetPosition =
            new(5, 1);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    targetPosition
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                targetPosition));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(12, 1)]
    [InlineData(1, -1)]
    [InlineData(1, 12)]
    public void Evaluate_WhenSourcePositionIsOutsideBattlefield_Throws(
        int sourceX,
        int sourceY)
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(
                    sourceX,
                    sourceY),
                new GridPosition(5, 5)));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(12, 1)]
    [InlineData(1, -1)]
    [InlineData(1, 12)]
    public void Evaluate_WhenTargetPositionIsOutsideBattlefield_Throws(
        int targetX,
        int targetY)
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(
                    targetX,
                    targetY)));
    }

    [Fact]
    public void Evaluate_WhenBattlefieldIsInvalid_Throws()
    {
        GridPosition duplicatePosition =
            new(3, 3);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    duplicatePosition,
                    duplicatePosition
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 5)));
    }

    [Fact]
    public void Evaluate_WithNullBattlefield_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            EncounterLineOfSightRules.Evaluate(
                null!,
                new GridPosition(1, 1),
                new GridPosition(5, 5)));
    }

    private static EncounterBattlefieldState
        CreateBattlefield(
            IReadOnlyList<GridPosition>?
                blockedPositions = null)
    {
        return new EncounterBattlefieldState
        {
            BattlefieldId =
                "battlefield.test",
            Width = 12,
            Height = 12,
            BlockedPositions =
                blockedPositions
                ?? Array.Empty<GridPosition>(),
            DifficultTerrainPositions =
                Array.Empty<GridPosition>()
        };
    }
}
