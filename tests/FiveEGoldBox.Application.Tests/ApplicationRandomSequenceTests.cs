using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Tests;

public sealed class ApplicationRandomSequenceTests
{
    /// One vector per die Core defines. The d6, d8, d12 and d20 values are the
    /// ones this test pinned before d4 and d10 were rollable, which is the
    /// evidence that widening the supported set did not disturb any existing
    /// sequence.
    [Theory]
    [InlineData(DieType.D4, 2)]
    [InlineData(DieType.D6, 1)]
    [InlineData(DieType.D8, 8)]
    [InlineData(DieType.D10, 9)]
    [InlineData(DieType.D12, 9)]
    [InlineData(DieType.D20, 12)]
    public void GenerateDie_WithKnownVector_ReturnsStableValue(
        DieType die,
        int expectedValue)
    {
        ApplicationRandomRoll result =
            ApplicationRandomSequence.GenerateDie(
                seed: 8675309,
                valuesConsumed: 0,
                die: die);

        Assert.Equal(1, result.Ordinal);
        Assert.Equal((int)die, result.Sides);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(1, result.UpdatedValuesConsumed);
    }

    /// Every die Core defines can be rolled. Written against the enum rather
    /// than a list so that a die added to Core fails here until Application
    /// can roll it.
    [Fact]
    public void GenerateDie_SupportsEveryDieCoreDefines()
    {
        foreach (DieType die in Enum.GetValues<DieType>())
        {
            ApplicationRandomRoll roll =
                ApplicationRandomSequence.GenerateDie(
                    8675309,
                    0,
                    die);

            Assert.InRange(roll.Value, 1, (int)die);
            Assert.Equal((int)die, roll.Sides);
        }
    }

    /// A sequence mixing the newly rollable dice with the existing ones.
    [Fact]
    public void GenerateDie_MixedSequenceIncludingNewDice_IsStable()
    {
        DieType[] dice =
        [
            DieType.D4,
            DieType.D10,
            DieType.D20,
            DieType.D4,
            DieType.D10
        ];
        int[] expected = [2, 10, 14, 3, 5];
        int cursor = 0;
        List<int> actual = [];

        foreach (DieType die in dice)
        {
            ApplicationRandomRoll roll =
                ApplicationRandomSequence.GenerateDie(
                    8675309,
                    cursor,
                    die);

            actual.Add(roll.Value);
            cursor = roll.UpdatedValuesConsumed;
        }

        Assert.Equal(expected, actual);
        Assert.Equal(5, cursor);
    }

    [Fact]
    public void GenerateDie_MixedSequence_IsStable()
    {
        DieType[] dice =
        [
            DieType.D20,
            DieType.D8,
            DieType.D6,
            DieType.D12,
            DieType.D20
        ];
        int[] expected = [12, 7, 1, 8, 16];
        int cursor = 0;
        List<int> actual = [];

        foreach (DieType die in dice)
        {
            ApplicationRandomRoll roll =
                ApplicationRandomSequence.GenerateDie(
                    8675309,
                    cursor,
                    die);

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
        DieType[] dice =
        [
            DieType.D20,
            DieType.D8,
            DieType.D6,
            DieType.D12,
            DieType.D20
        ];

        foreach (DieType die in dice.Take(3))
        {
            cursor = ApplicationRandomSequence.GenerateDie(
                8675309,
                cursor,
                die).UpdatedValuesConsumed;
        }

        ApplicationRandomRoll continued =
            ApplicationRandomSequence.GenerateDie(
                8675309,
                cursor,
                dice[3]);

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
                die: DieType.D20);
        ApplicationRandomRoll second =
            ApplicationRandomSequence.GenerateDie(
                8675309,
                valuesConsumed: 4,
                die: DieType.D20);

        Assert.Equal(first, second);
        Assert.Equal(5, first.Ordinal);
    }

    /// d4 and d10 used to be rejected here. What is rejected now is a value
    /// that is not a die at all.
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(100)]
    public void GenerateDie_WithUndefinedDieType_Throws(
        int undefinedDie)
    {
        Assert.False(
            ApplicationRandomSequence.IsSupported(
                (DieType)undefinedDie));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApplicationRandomSequence.GenerateDie(
                8675309,
                0,
                (DieType)undefinedDie));
    }

    [Fact]
    public void GenerateDie_WithNegativeCursor_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApplicationRandomSequence.GenerateDie(
                8675309,
                -1,
                DieType.D20));
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
                DieType.D20);
        ApplicationRandomRoll damageD8 =
            ApplicationRandomSequence.GenerateDie(
                8675309,
                combatD20.UpdatedValuesConsumed,
                DieType.D8);

        Assert.Equal([12, 10, 14, 1, 16], initiative);
        Assert.Equal(5, cursor);
        Assert.Equal(17, combatD20.Value);
        Assert.Equal(6, combatD20.Ordinal);
        Assert.Equal(3, damageD8.Value);
        Assert.Equal(7, damageD8.Ordinal);
    }
}
