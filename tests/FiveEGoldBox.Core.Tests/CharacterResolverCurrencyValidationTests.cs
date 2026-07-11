using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverCurrencyValidationTests
{
    [Fact]
    public void Validate_WithNonNegativeCurrencyAmounts_ReturnsValidResult()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            Currency = new CurrencyAmount
            {
                CopperPieces = 0,
                SilverPieces = 1,
                ElectrumPieces = 2,
                GoldPieces = 3,
                PlatinumPieces = 4
            }
        };

        CharacterResolver resolver = new();

        ValidationResult result = resolver.Validate(draft);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "character.currency.amount.invalid");
    }

    [Theory]
    [InlineData("CopperPieces")]
    [InlineData("SilverPieces")]
    [InlineData("ElectrumPieces")]
    [InlineData("GoldPieces")]
    [InlineData("PlatinumPieces")]
    public void Validate_WithNegativeCurrencyAmount_ReturnsInvalidCurrencyError(
        string currencyPropertyName)
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            Currency = CreateCurrencyWithNegativeAmount(currencyPropertyName)
        };

        CharacterResolver resolver = new();

        ValidationResult result = resolver.Validate(draft);

        Assert.False(result.IsValid);

        ValidationIssue issue = Assert.Single(
            result.Issues,
            issue => issue.Code == "character.currency.amount.invalid");

        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Contains(currencyPropertyName, issue.Message);
    }

    private static CurrencyAmount CreateCurrencyWithNegativeAmount(
        string currencyPropertyName)
    {
        return currencyPropertyName switch
        {
            "CopperPieces" => new CurrencyAmount
            {
                CopperPieces = -1
            },
            "SilverPieces" => new CurrencyAmount
            {
                SilverPieces = -1
            },
            "ElectrumPieces" => new CurrencyAmount
            {
                ElectrumPieces = -1
            },
            "GoldPieces" => new CurrencyAmount
            {
                GoldPieces = -1
            },
            "PlatinumPieces" => new CurrencyAmount
            {
                PlatinumPieces = -1
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(currencyPropertyName),
                currencyPropertyName,
                "Unsupported currency property name.")
        };
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Currency Validation Character",
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
