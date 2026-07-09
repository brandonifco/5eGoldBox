using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverHitPointTests
{
    [Fact]
    public void Resolve_WithSelectedClass_CalculatesLevelOneMaxHitPoints()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(13, draft.BaseAbilityScores[Ability.Constitution]);
        Assert.Equal(14, snapshot.AbilityScores[Ability.Constitution]);
        Assert.Equal(2, snapshot.AbilityModifiers[Ability.Constitution]);

        Assert.Equal(DieType.D10, snapshot.HitDie);
        Assert.Equal(12, snapshot.MaxHitPoints);
    }

    [Fact]
    public void Resolve_WithoutSelectedClass_LeavesMaxHitPointsNull()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = null,
            ClassId = null
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Null(snapshot.HitDie);
        Assert.Null(snapshot.MaxHitPoints);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Test Fighter",
            Level = 1,
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
                    BaseSpeedFeet = 30,
                    AbilityScoreIncreases =
                    [
                        new AbilityScoreIncrease(Ability.Strength, 1),
                        new AbilityScoreIncrease(Ability.Dexterity, 1),
                        new AbilityScoreIncrease(Ability.Constitution, 1),
                        new AbilityScoreIncrease(Ability.Intelligence, 1),
                        new AbilityScoreIncrease(Ability.Wisdom, 1),
                        new AbilityScoreIncrease(Ability.Charisma, 1)
                    ],
                    Languages =
                    [
                        "language.common"
                    ]
                }
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.fighter",
                    Name = "Fighter",
                    HitDie = DieType.D10,
                    SavingThrowProficiencies =
                    [
                        Ability.Strength,
                        Ability.Constitution
                    ]
                }
            ]
        };
    }
}