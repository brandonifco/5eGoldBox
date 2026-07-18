using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterCoverRulesTests
{
    [Fact]
    public void Evaluate_WithNoCoverAlongLine_ReturnsNoCover()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield();

        EncounterLineOfSightResult lineOfSight =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1));

        EncounterCoverEvaluation result =
            EncounterCoverRules.Evaluate(
                battlefield,
                lineOfSight);

        Assert.Equal(
            EncounterCoverLevel.None,
            result.CoverLevel);
        Assert.Equal(0, result.ArmorClassBonus);
        Assert.Equal(0, result.DexteritySavingThrowBonus);
        Assert.Null(result.CoverPosition);
    }

    [Fact]
    public void Evaluate_WithHalfCoverAlongLine_ReturnsHalfCover()
    {
        GridPosition coverPosition =
            new(3, 1);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                coverPositions:
                [
                    new EncounterCoverPosition
                    {
                        Position = coverPosition,
                        CoverLevel =
                            EncounterCoverLevel.Half
                    }
                ]);

        EncounterLineOfSightResult lineOfSight =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1));

        EncounterCoverEvaluation result =
            EncounterCoverRules.Evaluate(
                battlefield,
                lineOfSight);

        Assert.Equal(
            EncounterCoverLevel.Half,
            result.CoverLevel);
        Assert.Equal(2, result.ArmorClassBonus);
        Assert.Equal(2, result.DexteritySavingThrowBonus);
        Assert.Equal(
            coverPosition,
            result.CoverPosition);
    }

    [Fact]
    public void Evaluate_WithMultipleCoverPositions_ReturnsStrongestCover()
    {
        GridPosition halfCoverPosition =
            new(2, 1);

        GridPosition threeQuartersCoverPosition =
            new(4, 1);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                coverPositions:
                [
                    new EncounterCoverPosition
                    {
                        Position = halfCoverPosition,
                        CoverLevel =
                            EncounterCoverLevel.Half
                    },
                    new EncounterCoverPosition
                    {
                        Position =
                            threeQuartersCoverPosition,
                        CoverLevel =
                            EncounterCoverLevel
                                .ThreeQuarters
                    }
                ]);

        EncounterLineOfSightResult lineOfSight =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1));

        EncounterCoverEvaluation result =
            EncounterCoverRules.Evaluate(
                battlefield,
                lineOfSight);

        Assert.Equal(
            EncounterCoverLevel.ThreeQuarters,
            result.CoverLevel);
        Assert.Equal(5, result.ArmorClassBonus);
        Assert.Equal(5, result.DexteritySavingThrowBonus);
        Assert.Equal(
            threeQuartersCoverPosition,
            result.CoverPosition);
    }

    [Fact]
    public void Evaluate_WhenCoverIsOutsideLine_DoesNotApplyCover()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                coverPositions:
                [
                    new EncounterCoverPosition
                    {
                        Position =
                            new GridPosition(3, 2),
                        CoverLevel =
                            EncounterCoverLevel
                                .ThreeQuarters
                    }
                ]);

        EncounterLineOfSightResult lineOfSight =
            EncounterLineOfSightRules.Evaluate(
                battlefield,
                new GridPosition(1, 1),
                new GridPosition(5, 1));

        EncounterCoverEvaluation result =
            EncounterCoverRules.Evaluate(
                battlefield,
                lineOfSight);

        Assert.Equal(
            EncounterCoverLevel.None,
            result.CoverLevel);
        Assert.Equal(0, result.ArmorClassBonus);
        Assert.Equal(0, result.DexteritySavingThrowBonus);
        Assert.Null(result.CoverPosition);
    }

    private static EncounterBattlefieldState
        CreateBattlefield(
            IReadOnlyList<EncounterCoverPosition>?
                coverPositions = null)
    {
        return new EncounterBattlefieldState
        {
            BattlefieldId = "battlefield.test",
            Width = 12,
            Height = 12,
            BlockedPositions =
                Array.Empty<GridPosition>(),
            CoverPositions =
                coverPositions
                ?? Array.Empty<
                    EncounterCoverPosition>(),
            DifficultTerrainPositions =
                Array.Empty<GridPosition>()
        };
    }
}
