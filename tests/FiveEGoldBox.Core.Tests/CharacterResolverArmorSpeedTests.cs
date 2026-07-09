using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverArmorSpeedTests
{
    [Fact]
    public void Resolve_WithArmorStrengthRequirementMet_DoesNotReduceSpeed()
    {
        RulesetDefinition ruleset = CreateArmorRuleset();

        CharacterDraft draft = CreateStrongDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.chain_mail"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(30, snapshot.SpeedFeet);
    }

    [Fact]
    public void Resolve_WithArmorStrengthRequirementNotMet_ReducesSpeedByTenFeet()
    {
        RulesetDefinition ruleset = CreateArmorRuleset();

        CharacterDraft draft = CreateWeakDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.chain_mail"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(20, snapshot.SpeedFeet);
    }

    [Fact]
    public void Resolve_WithArmorThatHasNoStrengthRequirement_DoesNotReduceSpeed()
    {
        RulesetDefinition ruleset = CreateArmorRuleset();

        CharacterDraft draft = CreateWeakDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.leather"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(30, snapshot.SpeedFeet);
    }

    [Fact]
    public void Resolve_WithOnlyShieldEquipped_DoesNotReduceSpeed()
    {
        RulesetDefinition ruleset = CreateArmorRuleset();

        CharacterDraft draft = CreateWeakDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedShieldId = "armor.shield"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(30, snapshot.SpeedFeet);
    }

    private static CharacterDraft CreateStrongDraft()
    {
        return new CharacterDraft
        {
            Name = "Strong Fighter",
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

    private static CharacterDraft CreateWeakDraft()
    {
        return new CharacterDraft
        {
            Name = "Weak Fighter",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 8,
                [Ability.Dexterity] = 14,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 12,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 15
            }
        };
    }

    private static RulesetDefinition CreateArmorRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.armor_speed",
            Name = "Armor Speed Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                CreateFighterClass()
            ],
            Armors =
            [
                CreateLeatherArmor(),
                CreateChainMail(),
                CreateShield()
            ]
        };
    }

    private static RaceDefinition CreateHumanRace()
    {
        return new RaceDefinition
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
        };
    }

    private static ClassDefinition CreateFighterClass()
    {
        return new ClassDefinition
        {
            Id = "class.fighter",
            Name = "Fighter",
            HitDie = DieType.D10,
            ArmorProficiencies =
            [
                "armor.light",
                "armor.heavy",
                "armor.shields"
            ]
        };
    }

    private static ArmorDefinition CreateLeatherArmor()
    {
        return new ArmorDefinition
        {
            Id = "armor.leather",
            Name = "Leather",
            Category = ArmorCategory.Light,
            BaseArmorClass = 11,
            AddsDexterityModifier = true,
            WeightPounds = 10m
        };
    }

    private static ArmorDefinition CreateChainMail()
    {
        return new ArmorDefinition
        {
            Id = "armor.chain_mail",
            Name = "Chain Mail",
            Category = ArmorCategory.Heavy,
            BaseArmorClass = 16,
            StrengthRequirement = 13,
            HasStealthDisadvantage = true,
            WeightPounds = 55m
        };
    }

    private static ArmorDefinition CreateShield()
    {
        return new ArmorDefinition
        {
            Id = "armor.shield",
            Name = "Shield",
            Category = ArmorCategory.Shield,
            BaseArmorClass = 0,
            ArmorClassBonus = 2,
            WeightPounds = 6m
        };
    }
}