using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CurrencyModelTests
{
    [Fact]
    public void CurrencyAmount_DefaultsToZeroForAllCoinTypes()
    {
        CurrencyAmount currency = new();

        Assert.Equal(0, currency.CopperPieces);
        Assert.Equal(0, currency.SilverPieces);
        Assert.Equal(0, currency.ElectrumPieces);
        Assert.Equal(0, currency.GoldPieces);
        Assert.Equal(0, currency.PlatinumPieces);
    }

    [Fact]
    public void CharacterDraft_CanContainCurrency()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            Currency = new CurrencyAmount
            {
                CopperPieces = 10,
                SilverPieces = 20,
                ElectrumPieces = 30,
                GoldPieces = 40,
                PlatinumPieces = 50
            }
        };

        Assert.Equal(10, draft.Currency.CopperPieces);
        Assert.Equal(20, draft.Currency.SilverPieces);
        Assert.Equal(30, draft.Currency.ElectrumPieces);
        Assert.Equal(40, draft.Currency.GoldPieces);
        Assert.Equal(50, draft.Currency.PlatinumPieces);
    }

    [Fact]
    public void Resolve_CopiesCurrencyToSnapshot()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            Currency = new CurrencyAmount
            {
                CopperPieces = 1,
                SilverPieces = 2,
                ElectrumPieces = 3,
                GoldPieces = 4,
                PlatinumPieces = 5
            }
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(1, snapshot.Currency.CopperPieces);
        Assert.Equal(2, snapshot.Currency.SilverPieces);
        Assert.Equal(3, snapshot.Currency.ElectrumPieces);
        Assert.Equal(4, snapshot.Currency.GoldPieces);
        Assert.Equal(5, snapshot.Currency.PlatinumPieces);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Currency Character",
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