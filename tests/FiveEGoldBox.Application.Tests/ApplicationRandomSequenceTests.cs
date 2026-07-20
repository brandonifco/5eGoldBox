using FiveEGoldBox.Application.Randomness;

namespace FiveEGoldBox.Application.Tests;

public sealed class ApplicationRandomSequenceTests
{
    [Theory]
    [InlineData(6, 1)]
    [InlineData(8, 8)]
    [InlineData(12, 9)]
    [InlineData(20, 12)]
    public void GenerateDie_WithKnownVector_ReturnsStableValue(
        int sides,
        int expectedValue)
    {
        ApplicationRandomRoll result =
            ApplicationRandomSequence.GenerateDie(
                seed: 8675309,
                valuesConsumed: 0,
                sides: sides);

        Assert.Equal(1, result.Ordinal);
        Assert.Equal(sides, result.Sides);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(1, result.UpdatedValuesConsumed);
    }

    [Fact]
    public void GenerateDie_MixedSequence_IsStable()
    {
        int[] sides = [20, 8, 6, 12, 20];
        int[] expected = [12, 7, 1, 8, 16];
        int cursor = 0;
        List<int> actual = [];

        foreach (int dieSides in sides)
        {
            ApplicationRandomRoll roll =
                ApplicationRandomSequence.GenerateDie(
                    8675309,
                    cursor,
                    dieSides);

            actual.Add(roll.Value);
            cursor = roll.UpdatedValuesConsumed;
        }

        Assert.Equal(expected, actual);
        Assert.Equal(5, cursor);
    }

    [Fact]
    public void GenerateDie_ContinuationFromCursorMatchesUninterruptedSequence()
    {
        int cursor = 0;
        int[] sides = [20, 8, 6, 12, 20];

        foreach (int dieSides in sides.Take(3))
        {
            cursor = ApplicationRandomSequence.GenerateDie(
                8675309,
                cursor,
                dieSides).UpdatedValuesConsumed;
        }

        ApplicationRandomRoll continued =
            ApplicationRandomSequence.GenerateDie(
                8675309,
                cursor,
                sides[3]);

        Assert.Equal(4, continued.Ordinal);
        Assert.Equal(8, continued.Value);
    }

    [Fact]
    public void GenerateDie_PriorDieSizesAreNotRequiredBySavedCursor()
    {
        ApplicationRandomRoll first =
            ApplicationRandomSequence.GenerateDie(
                8675309,
                valuesConsumed: 4,
                sides: 20);
        ApplicationRandomRoll second =
            ApplicationRandomSequence.GenerateDie(
                8675309,
                valuesConsumed: 4,
                sides: 20);

        Assert.Equal(first, second);
        Assert.Equal(5, first.Ordinal);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(100)]
    public void GenerateDie_WithUnsupportedSides_Throws(
        int sides)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApplicationRandomSequence.GenerateDie(
                8675309,
                0,
                sides));
    }

    [Fact]
    public void GenerateDie_WithNegativeCursor_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApplicationRandomSequence.GenerateDie(
                8675309,
                -1,
                20));
    }

    [Fact]
    public void GenerateD20Rolls_UsesSameLogicalStream()
    {
        IReadOnlyList<int> initiative =
            ApplicationRandomSequence.GenerateD20Rolls(
                8675309,
                0,
                5,
                out int cursor);
        ApplicationRandomRoll combatD20 =
            ApplicationRandomSequence.GenerateDie(
                8675309,
                cursor,
                20);
        ApplicationRandomRoll damageD8 =
            ApplicationRandomSequence.GenerateDie(
                8675309,
                combatD20.UpdatedValuesConsumed,
                8);

        Assert.Equal([12, 10, 14, 1, 16], initiative);
        Assert.Equal(5, cursor);
        Assert.Equal(17, combatD20.Value);
        Assert.Equal(6, combatD20.Ordinal);
        Assert.Equal(3, damageD8.Value);
        Assert.Equal(7, damageD8.Ordinal);
    }
}
