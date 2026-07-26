using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Tests.Builders;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverArmorStealthTests
{
    [Fact]
    public void Resolve_WithStealthDisadvantageArmor_SetsHasStealthDisadvantageTrue()
    {
        RulesetDefinition ruleset = CreateArmorRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.chain_mail"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.True(snapshot.HasStealthDisadvantage);
    }

    [Fact]
    public void Resolve_WithArmorThatDoesNotCauseStealthDisadvantage_SetsHasStealthDisadvantageFalse()
    {
        RulesetDefinition ruleset = CreateArmorRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedArmorId = "armor.leather"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.False(snapshot.HasStealthDisadvantage);
    }

    [Fact]
    public void Resolve_WithOnlyShieldEquipped_SetsHasStealthDisadvantageFalse()
    {
        RulesetDefinition ruleset = CreateArmorRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedShieldId = "armor.shield"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.False(snapshot.HasStealthDisadvantage);
    }

    [Fact]
    public void Resolve_WithNoArmorEquipped_SetsHasStealthDisadvantageFalse()
    {
        RulesetDefinition ruleset = CreateArmorRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.False(snapshot.HasStealthDisadvantage);
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

    private static RulesetDefinition CreateArmorRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.armor_stealth",
            Name = "Armor Stealth Ruleset",
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
