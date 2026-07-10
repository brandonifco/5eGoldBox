using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverHitPointByLevelTests
{
    [Fact]
    public void Resolve_AtLevelOne_UsesMaximumHitDiePlusConstitutionModifier()
    {
        RulesetDefinition ruleset = CreateClassRuleset(DieType.D10);

        CharacterDraft draft = CreateConstitutionFourteenDraft() with
        {
            Level = 1,
            RaceId = "race.test",
            ClassId = "class.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(12, snapshot.MaxHitPoints);
    }

    [Fact]
    public void Resolve_AboveLevelOne_AddsFixedAverageHitPointsPerAdditionalLevel()
    {
        RulesetDefinition ruleset = CreateClassRuleset(DieType.D10);

        CharacterDraft draft = CreateConstitutionFourteenDraft() with
        {
            Level = 5,
            RaceId = "race.test",
            ClassId = "class.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(44, snapshot.MaxHitPoints);
    }

    [Theory]
    [InlineData(DieType.D6, 20)]
    [InlineData(DieType.D8, 24)]
    [InlineData(DieType.D10, 28)]
    [InlineData(DieType.D12, 32)]
    public void Resolve_UsesCorrectFixedAverageHitPointsForEachClassHitDie(
        DieType hitDie,
        int expectedMaxHitPoints)
    {
        RulesetDefinition ruleset = CreateClassRuleset(hitDie);

        CharacterDraft draft = CreateConstitutionFourteenDraft() with
        {
            Level = 3,
            RaceId = "race.test",
            ClassId = "class.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(expectedMaxHitPoints, snapshot.MaxHitPoints);
    }

    [Fact]
    public void Resolve_WithNegativeConstitutionModifier_AppliesModifierAtEachLevel()
    {
        RulesetDefinition ruleset = CreateClassRuleset(DieType.D8);

        CharacterDraft draft = CreateConstitutionEightDraft() with
        {
            Level = 4,
            RaceId = "race.test",
            ClassId = "class.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(19, snapshot.MaxHitPoints);
    }

    [Fact]
    public void Resolve_WithoutSelectedClass_LeavesMaxHitPointsNull()
    {
        CharacterDraft draft = CreateConstitutionFourteenDraft();

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Null(snapshot.MaxHitPoints);
    }

    private static CharacterDraft CreateConstitutionFourteenDraft()
    {
        return new CharacterDraft
        {
            Name = "Durable Character",
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

    private static CharacterDraft CreateConstitutionEightDraft()
    {
        return new CharacterDraft
        {
            Name = "Fragile Character",
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 8,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreateClassRuleset(DieType hitDie)
    {
        return new RulesetDefinition
        {
            Id = "ruleset.hit_points",
            Name = "Hit Points Ruleset",
            Races =
            [
                CreateTestRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.test",
                    Name = "Test Class",
                    HitDie = hitDie
                }
            ]
        };
    }

    private static RaceDefinition CreateTestRace()
    {
        return new RaceDefinition
        {
            Id = "race.test",
            Name = "Test Race",
            BaseSpeedFeet = 30
        };
    }
}