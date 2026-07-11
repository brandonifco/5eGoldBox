using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Tests.Builders;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverCurrencyValueTests
{
    [Fact]
    public void Resolve_WithNoCurrency_SetsCurrencyValueToZeroCopperPieces()
    {
        CharacterDraft draft = TestCharacterDraftBuilder.Valid();

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(0, snapshot.CurrencyValueInCopperPieces);
    }

    [Fact]
    public void Resolve_WithOnlyCopperPieces_UsesCopperPiecesAsValue()
    {
        CharacterDraft draft = TestCharacterDraftBuilder.Valid() with
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
        CharacterDraft draft = TestCharacterDraftBuilder.Valid() with
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
        CharacterDraft draft = TestCharacterDraftBuilder.Valid() with
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
}
