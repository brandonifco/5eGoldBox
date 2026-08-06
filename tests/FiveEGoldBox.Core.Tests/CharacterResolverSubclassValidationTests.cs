using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverSubclassValidationTests
{
    [Fact]
    public void Validate_WithNoSubclassChosen_ReturnsValidResult()
    {
        // Always legal, unlike a subrace on a race that has any -- a
        // level-1 character of a class whose real subclass choice happens
        // above level 1 simply hasn't picked one yet.
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            ClassId = "class.cleric",
            SubclassId = null
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithSubclassBelongingToItsOwnClass_ReturnsValidResult()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            ClassId = "class.cleric",
            SubclassId = "subclass.life-domain"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithSubclassBelongingToADifferentClass_ReturnsSubclassNotFoundError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            ClassId = "class.fighter",
            SubclassId = "subclass.life-domain"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.subclass.not_found");
    }

    [Fact]
    public void Validate_WithUnknownSubclass_ReturnsSubclassNotFoundError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            ClassId = "class.cleric",
            SubclassId = "subclass.not-real"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.subclass.not_found");
    }

    [Fact]
    public void Validate_WithSubclassAndNoClass_ReturnsSubclassRequiresClassError()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            ClassId = null,
            SubclassId = "subclass.life-domain"
        };

        CharacterResolver resolver = new(ruleset);

        var result = resolver.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.subclass.requires_class");
    }

    [Fact]
    public void Resolve_WithSubclassChosen_PopulatesSubclassNameOnTheSnapshot()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            ClassId = "class.cleric",
            SubclassId = "subclass.war-domain"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal("subclass.war-domain", snapshot.SubclassId);
        Assert.Equal("War Domain", snapshot.SubclassName);
    }

    [Fact]
    public void Resolve_WithNoSubclassChosen_LeavesSubclassNullOnTheSnapshot()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            ClassId = "class.cleric",
            SubclassId = null
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Null(snapshot.SubclassId);
        Assert.Null(snapshot.SubclassName);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Test Character",
            Level = 1,
            RaceId = "race.human",
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.StandardArray,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 14,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 12,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 8
            }
        };
    }

    private static RulesetDefinition CreateTestRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.human",
                    Name = "Human",
                    BaseSpeedFeet = 30
                }
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.cleric",
                    Name = "Cleric",
                    HitDie = DieType.D8,
                    Subclasses =
                    [
                        new SubclassDefinition
                        {
                            Id = "subclass.life-domain",
                            Name = "Life Domain"
                        },
                        new SubclassDefinition
                        {
                            Id = "subclass.war-domain",
                            Name = "War Domain"
                        }
                    ]
                },
                new ClassDefinition
                {
                    Id = "class.fighter",
                    Name = "Fighter",
                    HitDie = DieType.D10,
                    Subclasses =
                    [
                        new SubclassDefinition
                        {
                            Id = "subclass.champion",
                            Name = "Champion"
                        }
                    ]
                }
            ]
        };
    }
}
