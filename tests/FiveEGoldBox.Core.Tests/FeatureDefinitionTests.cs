using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

/// Class features stopping being inert strings. `FeaturesByLevel` has held IDs
/// since the beginning and nothing read them; now they name something, so a
/// ruleset has to declare what they name and a resolved character carries what
/// they do.
public sealed class FeatureDefinitionTests
{
    [Fact]
    public void Validate_AClassNamingAnUndeclaredFeature_IsAnError()
    {
        ValidationResult result = RulesetValidator.Validate(
            CreateRuleset(
                features: Array.Empty<FeatureDefinition>(),
                classFeatureId: "feature.sneak_attack"));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code
                == "ruleset.classes.features.feature_unknown");
    }

    [Fact]
    public void Validate_ABackgroundNamingAnUndeclaredFeature_IsAnError()
    {
        RulesetDefinition ruleset = CreateRuleset();

        ValidationResult result = RulesetValidator.Validate(
            ruleset with
            {
                Backgrounds =
                [
                    new BackgroundDefinition
                    {
                        Id = "background.test",
                        Name = "Test Background",
                        FeatureId = "feature.nowhere"
                    }
                ]
            });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code
                == "ruleset.backgrounds.feature_unknown");
    }

    [Fact]
    public void Validate_AClassNamingADeclaredFeature_IsFine()
    {
        Assert.True(
            RulesetValidator.Validate(CreateRuleset()).IsValid);
    }

    /// A feature that contributes nothing is as inert as the bare string ID it
    /// replaced. Second Wind will sit here until the resource and action legs
    /// are built, so this must not be an error.
    [Fact]
    public void Validate_AFeatureThatContributesNothing_IsFine()
    {
        Assert.True(
            RulesetValidator.Validate(
                CreateRuleset(
                    features:
                    [
                        new FeatureDefinition
                        {
                            Id = "feature.sneak_attack",
                            Name = "Sneak Attack"
                        }
                    ]))
                .IsValid);
    }

    [Theory]
    [InlineData(0, DieType.D6, "ruleset.features.dice_count_invalid")]
    [InlineData(1, (DieType)7, "ruleset.features.die_undefined")]
    public void Validate_AFeatureRollingWhatItCannot_IsAnError(
        int diceCount,
        DieType die,
        string expectedCode)
    {
        ValidationResult result = RulesetValidator.Validate(
            CreateRuleset(
                features:
                [
                    new FeatureDefinition
                    {
                        Id = "feature.sneak_attack",
                        Name = "Sneak Attack",
                        Contributions =
                        [
                            new RollContributionDefinition
                            {
                                Target =
                                    RollContributionTarget.DamageRoll,
                                Dice = new DamageDice
                                {
                                    Count = diceCount,
                                    Die = die
                                }
                            }
                        ]
                    }
                ]));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == expectedCode);
    }

    [Fact]
    public void Validate_AFeatureAskingAnUndefinedCondition_IsAnError()
    {
        ValidationResult result = RulesetValidator.Validate(
            CreateRuleset(
                features:
                [
                    CreateSneakAttack() with
                    {
                        Contributions =
                        [
                            new RollContributionDefinition
                            {
                                Target =
                                    RollContributionTarget.DamageRoll,
                                FlatBonus = 2,
                                Conditions =
                                [
                                    (RollContributionCondition)9
                                ]
                            }
                        ]
                    }
                ]));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code
                == "ruleset.features.condition_undefined");
    }

    /// The point of declaring them: what a class feature does reaches the
    /// resolved character, so nothing downstream needs a ruleset in hand.
    [Fact]
    public void Resolve_ACharacterCarriesWhatItsFeaturesContribute()
    {
        CharacterSnapshot snapshot = Resolve(CreateRuleset());

        Assert.Equal(
            ["feature.sneak_attack"],
            snapshot.ClassFeatures);

        RollContributionDefinition contribution = Assert.Single(
            snapshot.Contributions);

        Assert.Equal(
            RollContributionTarget.DamageRoll,
            contribution.Target);
        Assert.Equal(DieType.D6, contribution.Dice!.Die);
        Assert.Equal(
            [
                RollContributionCondition.AdvantageOrAdjacentEnemy,
                RollContributionCondition.FinesseOrRangedWeapon
            ],
            contribution.Conditions);
    }

    /// A resolver built without a ruleset is a supported way to make a
    /// character, and it has nothing to look a feature up in.
    [Fact]
    public void Resolve_WithoutARuleset_CarriesNoContributions()
    {
        Assert.Empty(
            new CharacterResolver()
                .Resolve(CreateDraft())
                .Contributions);
    }

    private static CharacterSnapshot Resolve(
        RulesetDefinition ruleset)
    {
        return new CharacterResolver(ruleset).Resolve(CreateDraft());
    }

    private static CharacterDraft CreateDraft()
    {
        return new CharacterDraft
        {
            Name = "Test Rogue",
            Level = 1,
            RaceId = "race.test",
            ClassId = "class.test",
            AbilityScoreGenerationMethod =
                AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 15,
                [Ability.Constitution] = 12,
                [Ability.Intelligence] = 11,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 9
            },
            SelectedSkillIds = ["skill.test"]
        };
    }

    private static FeatureDefinition CreateSneakAttack()
    {
        return new FeatureDefinition
        {
            Id = "feature.sneak_attack",
            Name = "Sneak Attack",
            Contributions =
            [
                new RollContributionDefinition
                {
                    Target = RollContributionTarget.DamageRoll,
                    Dice = new DamageDice
                    {
                        Count = 1,
                        Die = DieType.D6
                    },
                    Conditions =
                    [
                        RollContributionCondition
                            .AdvantageOrAdjacentEnemy,
                        RollContributionCondition
                            .FinesseOrRangedWeapon
                    ]
                }
            ]
        };
    }

    private static RulesetDefinition CreateRuleset(
        IReadOnlyList<FeatureDefinition>? features = null,
        string classFeatureId = "feature.sneak_attack")
    {
        return new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
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
                    HitDie = DieType.D8,
                    SkillChoices = ["skill.test"],
                    NumberOfSkillChoices = 1,
                    FeaturesByLevel =
                        new Dictionary<int, IReadOnlyList<string>>
                        {
                            [1] = [classFeatureId]
                        }
                }
            ],
            Skills =
            [
                new SkillDefinition
                {
                    Id = "skill.test",
                    Name = "Test Skill",
                    Ability = Ability.Dexterity
                }
            ],
            Features = features ?? [CreateSneakAttack()]
        };
    }
}
