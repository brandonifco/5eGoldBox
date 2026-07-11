using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverSkillDisadvantageTests
{
    [Fact]
    public void Resolve_WithStealthDisadvantageArmor_SetsStealthSkillDisadvantageTrue()
    {
        RulesetDefinition ruleset = CreateSkillAndArmorRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.chain_mail"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SkillBonus stealth = GetSkillBonus(snapshot, "skill.stealth");

        Assert.True(snapshot.HasStealthDisadvantage);
        Assert.True(stealth.HasDisadvantage);
    }

    [Fact]
    public void Resolve_WithArmorThatDoesNotCauseStealthDisadvantage_SetsStealthSkillDisadvantageFalse()
    {
        RulesetDefinition ruleset = CreateSkillAndArmorRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.leather"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SkillBonus stealth = GetSkillBonus(snapshot, "skill.stealth");

        Assert.False(snapshot.HasStealthDisadvantage);
        Assert.False(stealth.HasDisadvantage);
    }

    [Fact]
    public void Resolve_WithOnlyShieldEquipped_SetsStealthSkillDisadvantageFalse()
    {
        RulesetDefinition ruleset = CreateSkillAndArmorRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedShieldId = "armor.shield"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SkillBonus stealth = GetSkillBonus(snapshot, "skill.stealth");

        Assert.False(snapshot.HasStealthDisadvantage);
        Assert.False(stealth.HasDisadvantage);
    }

    [Fact]
    public void Resolve_WithNoArmorEquipped_SetsStealthSkillDisadvantageFalse()
    {
        RulesetDefinition ruleset = CreateSkillAndArmorRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SkillBonus stealth = GetSkillBonus(snapshot, "skill.stealth");

        Assert.False(snapshot.HasStealthDisadvantage);
        Assert.False(stealth.HasDisadvantage);
    }

    [Fact]
    public void Resolve_WithStealthDisadvantageArmor_DoesNotSetOtherSkillsToDisadvantage()
    {
        RulesetDefinition ruleset = CreateSkillAndArmorRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.chain_mail"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SkillBonus perception = GetSkillBonus(snapshot, "skill.perception");

        Assert.True(snapshot.HasStealthDisadvantage);
        Assert.False(perception.HasDisadvantage);
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
            Name = "Skill Disadvantage Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 14,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 12,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreateSkillAndArmorRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.skill_disadvantage",
            Name = "Skill Disadvantage Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.test",
                    Name = "Test Race",
                    BaseSpeedFeet = 30
                }
            ],
            Skills =
            [
                new SkillDefinition
                {
                    Id = "skill.stealth",
                    Name = "Stealth",
                    Ability = Ability.Dexterity
                },
                new SkillDefinition
                {
                    Id = "skill.perception",
                    Name = "Perception",
                    Ability = Ability.Wisdom
                }
            ],
            Armors =
            [
                CreateLeatherArmor(),
                CreateChainMail(),
                CreateShield()
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
