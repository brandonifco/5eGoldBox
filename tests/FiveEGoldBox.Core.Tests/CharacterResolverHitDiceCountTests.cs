using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverHitDiceCountTests
{
    [Fact]
    public void Resolve_WithSelectedClassAtLevelOne_SetsHitDiceCountToOne()
    {
        RulesetDefinition ruleset = CreateClassRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            Level = 1,
            RaceId = "race.test",
            ClassId = "class.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(DieType.D10, snapshot.HitDie);
        Assert.Equal(1, snapshot.HitDiceCount);
    }

    [Fact]
    public void Resolve_WithSelectedClassAboveLevelOne_SetsHitDiceCountToLevel()
    {
        RulesetDefinition ruleset = CreateClassRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            Level = 5,
            RaceId = "race.test",
            ClassId = "class.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(DieType.D10, snapshot.HitDie);
        Assert.Equal(5, snapshot.HitDiceCount);
    }

    [Fact]
    public void Resolve_WithSelectedClassAtLevelTwenty_SetsHitDiceCountToTwenty()
    {
        RulesetDefinition ruleset = CreateClassRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            Level = 20,
            RaceId = "race.test",
            ClassId = "class.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(DieType.D10, snapshot.HitDie);
        Assert.Equal(20, snapshot.HitDiceCount);
    }

    [Fact]
    public void Resolve_WithoutSelectedClass_LeavesHitDiceCountNull()
    {
        CharacterDraft draft = CreateValidDraft();

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Null(snapshot.HitDie);
        Assert.Null(snapshot.HitDiceCount);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Hit Dice Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 14,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreateClassRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.hit_dice_count",
            Name = "Hit Dice Count Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.test",
                    Name = "Test Race",
                    BaseSpeedFeet = 30
                }
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.test",
                    Name = "Test Class",
                    HitDie = DieType.D10
                }
            ]
        };
    }
}