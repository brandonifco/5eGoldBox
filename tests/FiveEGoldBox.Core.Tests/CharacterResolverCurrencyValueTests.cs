using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverCurrencyValueTests
{
    [Fact]
    public void Resolve_WithNoCurrency_SetsCurrencyValueToZeroCopperPieces()
    {
        CharacterDraft draft = CreateValidDraft();

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(0, snapshot.CurrencyValueInCopperPieces);
    }

    [Fact]
    public void Resolve_WithOnlyCopperPieces_UsesCopperPiecesAsValue()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            Currency = new CurrencyAmount
            {
                CopperPieces = 37
            }
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(37, snapshot.CurrencyValueInCopperPieces);
    }

    [Fact]
    public void Resolve_WithEachCoinType_UsesCorrectCopperPieceConversionRates()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            Currency = new CurrencyAmount
            {
                CopperPieces = 1,
                SilverPieces = 1,
                ElectrumPieces = 1,
                GoldPieces = 1,
                PlatinumPieces = 1
            }
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(1161, snapshot.CurrencyValueInCopperPieces);
    }

    [Fact]
    public void Resolve_WithMixedCurrencyTotals_AllCoinValues()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            Currency = new CurrencyAmount
            {
                CopperPieces = 7,
                SilverPieces = 6,
                ElectrumPieces = 5,
                GoldPieces = 4,
                PlatinumPieces = 3
            }
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(3717, snapshot.CurrencyValueInCopperPieces);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Currency Value Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 10
            }
        };
    }
}