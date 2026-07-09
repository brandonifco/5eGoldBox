using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverSkillBonusTests
{
    [Fact]
    public void Resolve_WithClassSkillProficiency_CalculatesProficientSkillBonus()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            BackgroundId = "background.folk_hero",
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SkillBonus athletics = GetSkillBonus(snapshot, "skill.athletics");

        Assert.Equal("Athletics", athletics.SkillName);
        Assert.Equal(Ability.Strength, athletics.Ability);
        Assert.True(athletics.IsProficient);
        Assert.Equal(3, athletics.AbilityModifier);
        Assert.Equal(2, athletics.ProficiencyBonus);
        Assert.Equal(5, athletics.TotalBonus);
    }

    [Fact]
    public void Resolve_WithBackgroundSkillProficiency_CalculatesProficientSkillBonus()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            BackgroundId = "background.folk_hero",
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SkillBonus survival = GetSkillBonus(snapshot, "skill.survival");

        Assert.Equal("Survival", survival.SkillName);
        Assert.Equal(Ability.Wisdom, survival.Ability);
        Assert.True(survival.IsProficient);
        Assert.Equal(0, survival.AbilityModifier);
        Assert.Equal(2, survival.ProficiencyBonus);
        Assert.Equal(2, survival.TotalBonus);
    }

    [Fact]
    public void Resolve_WithNonProficientSkill_UsesOnlyAbilityModifier()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            BackgroundId = "background.folk_hero",
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SkillBonus stealth = GetSkillBonus(snapshot, "skill.stealth");

        Assert.Equal("Stealth", stealth.SkillName);
        Assert.Equal(Ability.Dexterity, stealth.Ability);
        Assert.False(stealth.IsProficient);
        Assert.Equal(2, stealth.AbilityModifier);
        Assert.Equal(0, stealth.ProficiencyBonus);
        Assert.Equal(2, stealth.TotalBonus);
    }

    [Fact]
    public void Resolve_WithoutRuleset_LeavesSkillBonusesEmpty()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = null,
            ClassId = null,
            BackgroundId = null,
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ]
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Empty(snapshot.SkillBonuses);
    }

    private static SkillBonus GetSkillBonus(CharacterSnapshot snapshot, string skillId)
    {
        return Assert.Single(
            snapshot.SkillBonuses,
            skill => skill.SkillId == skillId);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Test Skill Character",
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
                    ],
                    SkillChoices =
                    [
                        "skill.acrobatics",
                        "skill.animal_handling",
                        "skill.athletics",
                        "skill.history",
                        "skill.insight",
                        "skill.intimidation",
                        "skill.perception",
                        "skill.survival"
                    ],
                    NumberOfSkillChoices = 2
                }
            ],
            Backgrounds =
            [
                new BackgroundDefinition
                {
                    Id = "background.folk_hero",
                    Name = "Folk Hero",
                    SkillProficiencies =
                    [
                        "skill.animal_handling",
                        "skill.survival"
                    ],
                    FeatureId = "feature.rustic_hospitality"
                }
            ],
            Skills =
            [
                new SkillDefinition
                {
                    Id = "skill.athletics",
                    Name = "Athletics",
                    Ability = Ability.Strength
                },
                new SkillDefinition
                {
                    Id = "skill.perception",
                    Name = "Perception",
                    Ability = Ability.Wisdom
                },
                new SkillDefinition
                {
                    Id = "skill.survival",
                    Name = "Survival",
                    Ability = Ability.Wisdom
                },
                new SkillDefinition
                {
                    Id = "skill.stealth",
                    Name = "Stealth",
                    Ability = Ability.Dexterity
                }
            ]
        };
    }
}