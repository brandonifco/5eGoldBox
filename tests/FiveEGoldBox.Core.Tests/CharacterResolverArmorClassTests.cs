using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverArmorClassTests
{
    [Fact]
    public void Resolve_WithNoArmor_CalculatesUnarmoredArmorClass()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = null,
            EquippedShieldId = null
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(15, snapshot.AbilityScores[Ability.Dexterity]);
        Assert.Equal(2, snapshot.AbilityModifiers[Ability.Dexterity]);
        Assert.Equal(12, snapshot.ArmorClass);
    }

    [Fact]
    public void Resolve_WithLightArmor_AddsFullDexterityModifier()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.leather",
            EquippedShieldId = null
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal("armor.leather", snapshot.EquippedArmorId);
        Assert.Equal("Leather", snapshot.EquippedArmorName);
        Assert.Equal(13, snapshot.ArmorClass);
    }

    [Fact]
    public void Resolve_WithMediumArmor_CapsDexterityModifier()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateHighDexterityDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.scale_mail",
            EquippedShieldId = null
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(18, snapshot.AbilityScores[Ability.Dexterity]);
        Assert.Equal(4, snapshot.AbilityModifiers[Ability.Dexterity]);
        Assert.Equal(16, snapshot.ArmorClass);
    }

    [Fact]
    public void Resolve_WithHeavyArmor_DoesNotAddDexterityModifier()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateHighDexterityDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.chain_mail",
            EquippedShieldId = null
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(18, snapshot.AbilityScores[Ability.Dexterity]);
        Assert.Equal(4, snapshot.AbilityModifiers[Ability.Dexterity]);
        Assert.Equal(16, snapshot.ArmorClass);
    }

    [Fact]
    public void Resolve_WithShield_AddsShieldArmorClassBonus()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.chain_mail",
            EquippedShieldId = "armor.shield"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal("armor.chain_mail", snapshot.EquippedArmorId);
        Assert.Equal("Chain Mail", snapshot.EquippedArmorName);
        Assert.Equal("armor.shield", snapshot.EquippedShieldId);
        Assert.Equal("Shield", snapshot.EquippedShieldName);
        Assert.Equal(18, snapshot.ArmorClass);
    }

    [Fact]
    public void Resolve_WithoutRuleset_UsesUnarmoredArmorClass()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            EquippedArmorId = "armor.chain_mail",
            EquippedShieldId = "armor.shield"
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Null(snapshot.EquippedArmorId);
        Assert.Null(snapshot.EquippedArmorName);
        Assert.Null(snapshot.EquippedShieldId);
        Assert.Null(snapshot.EquippedShieldName);
        Assert.Equal(12, snapshot.ArmorClass);
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

    private static CharacterDraft CreateHighDexterityDraft()
    {
        return new CharacterDraft
        {
            Name = "High Dexterity Fighter",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 17,
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
            ],
            Armors =
            [
                new ArmorDefinition
                {
                    Id = "armor.leather",
                    Name = "Leather",
                    Category = ArmorCategory.Light,
                    BaseArmorClass = 11,
                    AddsDexterityModifier = true,
                    WeightPounds = 10m
                },
                new ArmorDefinition
                {
                    Id = "armor.scale_mail",
                    Name = "Scale Mail",
                    Category = ArmorCategory.Medium,
                    BaseArmorClass = 14,
                    AddsDexterityModifier = true,
                    MaximumDexterityModifier = 2,
                    HasStealthDisadvantage = true,
                    WeightPounds = 45m
                },
                new ArmorDefinition
                {
                    Id = "armor.chain_mail",
                    Name = "Chain Mail",
                    Category = ArmorCategory.Heavy,
                    BaseArmorClass = 16,
                    AddsDexterityModifier = false,
                    StrengthRequirement = 13,
                    HasStealthDisadvantage = true,
                    WeightPounds = 55m
                },
                new ArmorDefinition
                {
                    Id = "armor.shield",
                    Name = "Shield",
                    Category = ArmorCategory.Shield,
                    BaseArmorClass = 0,
                    ArmorClassBonus = 2,
                    WeightPounds = 6m
                }
            ]
        };
    }
}