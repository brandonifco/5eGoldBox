using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class StandardArrayRulesTests
{
    [Fact]
    public void IsValid_WithExactStandardArray_ReturnsTrue()
    {
        Dictionary<Ability, int> scores = new()
        {
            [Ability.Strength] = 15,
            [Ability.Dexterity] = 14,
            [Ability.Constitution] = 13,
            [Ability.Intelligence] = 12,
            [Ability.Wisdom] = 10,
            [Ability.Charisma] = 8
        };

        bool actual = StandardArrayRules.IsValid(scores);

        Assert.True(actual);
    }

    [Fact]
    public void IsValid_WithStandardArrayInDifferentAbilityOrder_ReturnsTrue()
    {
        Dictionary<Ability, int> scores = new()
        {
            [Ability.Strength] = 8,
            [Ability.Dexterity] = 15,
            [Ability.Constitution] = 14,
            [Ability.Intelligence] = 13,
            [Ability.Wisdom] = 12,
            [Ability.Charisma] = 10
        };

        bool actual = StandardArrayRules.IsValid(scores);

        Assert.True(actual);
    }

    [Fact]
    public void IsValid_WithDuplicateScore_ReturnsFalse()
    {
        Dictionary<Ability, int> scores = new()
        {
            [Ability.Strength] = 15,
            [Ability.Dexterity] = 15,
            [Ability.Constitution] = 13,
            [Ability.Intelligence] = 12,
            [Ability.Wisdom] = 10,
            [Ability.Charisma] = 8
        };

        bool actual = StandardArrayRules.IsValid(scores);

        Assert.False(actual);
    }

    [Fact]
    public void IsValid_WithMissingAbility_ReturnsFalse()
    {
        Dictionary<Ability, int> scores = new()
        {
            [Ability.Strength] = 15,
            [Ability.Dexterity] = 14,
            [Ability.Constitution] = 13,
            [Ability.Intelligence] = 12,
            [Ability.Wisdom] = 10
        };

        bool actual = StandardArrayRules.IsValid(scores);

        Assert.False(actual);
    }

    [Fact]
    public void IsValid_WithNullScores_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => StandardArrayRules.IsValid(null!));
    }
}