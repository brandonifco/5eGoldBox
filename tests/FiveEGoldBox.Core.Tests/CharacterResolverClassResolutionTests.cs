using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverClassResolutionTests
{
    [Fact]
    public void Resolve_WithSelectedClass_SetsClassMetadataAndHitDie()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal("class.fighter", snapshot.ClassId);
        Assert.Equal("Fighter", snapshot.ClassName);
        Assert.Equal(DieType.D10, snapshot.HitDie);
    }

    [Fact]
    public void Resolve_WithSelectedClass_SetsClassProficiencies()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Contains(Ability.Strength, snapshot.SavingThrowProficiencies);
        Assert.Contains(Ability.Constitution, snapshot.SavingThrowProficiencies);

        Assert.Contains("armor.light", snapshot.ArmorProficiencies);
        Assert.Contains("armor.medium", snapshot.ArmorProficiencies);
        Assert.Contains("armor.heavy", snapshot.ArmorProficiencies);
        Assert.Contains("armor.shields", snapshot.ArmorProficiencies);

        Assert.Contains("weapon.simple", snapshot.WeaponProficiencies);
        Assert.Contains("weapon.martial", snapshot.WeaponProficiencies);
    }

    [Fact]
    public void Resolve_WithSelectedClass_IncludesClassFeaturesUpToCurrentLevel()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            Level = 2,
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Contains("feature.fighting_style", snapshot.ClassFeatures);
        Assert.Contains("feature.second_wind", snapshot.ClassFeatures);
        Assert.Contains("feature.action_surge", snapshot.ClassFeatures);

        Assert.DoesNotContain("feature.martial_archetype", snapshot.ClassFeatures);
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
                    ],
                    ArmorProficiencies =
                    [
                        "armor.light",
                        "armor.medium",
                        "armor.heavy",
                        "armor.shields"
                    ],
                    WeaponProficiencies =
                    [
                        "weapon.simple",
                        "weapon.martial"
                    ],
                    FeaturesByLevel = new Dictionary<int, IReadOnlyList<string>>
                    {
                        [1] =
                        [
                            "feature.fighting_style",
                            "feature.second_wind"
                        ],
                        [2] =
                        [
                            "feature.action_surge"
                        ],
                        [3] =
                        [
                            "feature.martial_archetype"
                        ]
                    }
                }
            ]
        };
    }
}
