using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterBattlefieldCoverValidationTests
{
    [Fact]
    public void Evaluate_WithDuplicateCoverPositions_Throws()
    {
        GridPosition duplicatePosition =
            new(3, 1);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                coverPositions:
                [
                    new EncounterCoverPosition
                    {
                        Position = duplicatePosition,
                        CoverLevel =
                            EncounterCoverLevel.Half
                    },
                    new EncounterCoverPosition
                    {
                        Position = duplicatePosition,
                        CoverLevel =
                            EncounterCoverLevel
                                .ThreeQuarters
                    }
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1)));
    }

    [Fact]
    public void Evaluate_WithCoverOutsideBattlefield_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                coverPositions:
                [
                    new EncounterCoverPosition
                    {
                        Position =
                            new GridPosition(12, 1),
                        CoverLevel =
                            EncounterCoverLevel.Half
                    }
                ]);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1)));
    }

    [Fact]
    public void Evaluate_WithNoCoverLevel_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                coverPositions:
                [
                    new EncounterCoverPosition
                    {
                        Position =
                            new GridPosition(3, 1),
                        CoverLevel =
                            EncounterCoverLevel.None
                    }
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1)));
    }

    [Fact]
    public void Evaluate_WithUnsupportedCoverLevel_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                coverPositions:
                [
                    new EncounterCoverPosition
                    {
                        Position =
                            new GridPosition(3, 1),
                        CoverLevel =
                            (EncounterCoverLevel)999
                    }
                ]);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1)));
    }

    [Fact]
    public void Evaluate_WhenPositionIsBlockedAndPartialCover_Throws()
    {
        GridPosition overlappingPosition =
            new(3, 1);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    overlappingPosition
                ],
                coverPositions:
                [
                    new EncounterCoverPosition
                    {
                        Position = overlappingPosition,
                        CoverLevel =
                            EncounterCoverLevel.Half
                    }
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1)));
    }

    [Fact]
    public void Evaluate_WithNullCoverPosition_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                coverPositions:
                [
                    null!
                ]);

        Assert.Throws<ArgumentNullException>(() =>
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1)));
    }

    private static EncounterBattlefieldState
        CreateBattlefield(
            IReadOnlyList<GridPosition>?
                blockedPositions = null,
            IReadOnlyList<EncounterCoverPosition>?
                coverPositions = null)
    {
        return new EncounterBattlefieldState
        {
            BattlefieldId = "battlefield.test",
            Width = 12,
            Height = 12,
            BlockedPositions =
                blockedPositions
                ?? Array.Empty<GridPosition>(),
            CoverPositions =
                coverPositions
                ?? Array.Empty<
                    EncounterCoverPosition>(),
            DifficultTerrainPositions =
                Array.Empty<GridPosition>()
        };
    }
}
