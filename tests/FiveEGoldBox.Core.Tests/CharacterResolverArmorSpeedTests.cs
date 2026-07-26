using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Tests.Builders;

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
                TestRulesetBuilder.HumanRace()
            ],
            Classes =
            [
                CreateFighterClass()
            ],
            Armors =
            [
                TestRulesetBuilder.LeatherArmor(),
                CreateChainMail(),
                TestRulesetBuilder.Shield()
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

}
