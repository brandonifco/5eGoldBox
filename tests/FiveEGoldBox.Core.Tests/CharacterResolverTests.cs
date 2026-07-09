using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverTests
{
    [Fact]
    public void Resolve_WithValidDraft_ReturnsSnapshot()
    {
        CharacterDraft draft = new()
        {
            Name = "Bruenor",
            Level = 1,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 17,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 16,
                [Ability.Intelligence] = 8,
                [Ability.Wisdom] = 13,
                [Ability.Charisma] = 12
            }
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal("Bruenor", snapshot.Name);
        Assert.Equal(1, snapshot.Level);
        Assert.Equal(2, snapshot.ProficiencyBonus);

        Assert.Equal(17, snapshot.AbilityScores[Ability.Strength]);
        Assert.Equal(10, snapshot.AbilityScores[Ability.Dexterity]);
        Assert.Equal(16, snapshot.AbilityScores[Ability.Constitution]);
        Assert.Equal(8, snapshot.AbilityScores[Ability.Intelligence]);
        Assert.Equal(13, snapshot.AbilityScores[Ability.Wisdom]);
        Assert.Equal(12, snapshot.AbilityScores[Ability.Charisma]);

        Assert.Equal(3, snapshot.AbilityModifiers[Ability.Strength]);
        Assert.Equal(0, snapshot.AbilityModifiers[Ability.Dexterity]);
        Assert.Equal(3, snapshot.AbilityModifiers[Ability.Constitution]);
        Assert.Equal(-1, snapshot.AbilityModifiers[Ability.Intelligence]);
        Assert.Equal(1, snapshot.AbilityModifiers[Ability.Wisdom]);
        Assert.Equal(1, snapshot.AbilityModifiers[Ability.Charisma]);
    }

    [Fact]
    public void Validate_WithMissingAbilityScores_ReturnsSixMissingAbilityErrors()
    {
        CharacterDraft draft = new()
        {
            Name = "Incomplete Character",
            Level = 1,
            BaseAbilityScores = new Dictionary<Ability, int>()
        };

        CharacterResolver resolver = new();

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Equal(6, result.Issues.Count(issue => issue.Code.EndsWith(".missing")));
    }

    [Fact]
    public void Resolve_WithInvalidDraft_ThrowsInvalidOperationException()
    {
        CharacterDraft draft = new()
        {
            Name = "",
            Level = 1,
            BaseAbilityScores = new Dictionary<Ability, int>()
        };

        CharacterResolver resolver = new();

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(draft));
    }
}