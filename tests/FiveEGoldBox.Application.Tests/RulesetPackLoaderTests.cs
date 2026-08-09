using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class RulesetPackLoaderTests
{
    private const string BasePackWithFullContent = """
        {
            "FormatVersion": 1,
            "Id": "ruleset.test",
            "Name": "Test Ruleset",
            "Races": [
                {
                    "Id": "race.test",
                    "Name": "Test Race",
                    "Size": "Small",
                    "BaseSpeedFeet": 25,
                    "AbilityScoreIncreases": [
                        { "Ability": "Dexterity", "Amount": 2 }
                    ],
                    "Languages": [ "Common" ],
                    "Traits": [ "Test Trait" ],
                    "Subraces": [
                        {
                            "Id": "race.test.sub",
                            "Name": "Test Subrace",
                            "AbilityScoreIncreases": [
                                { "Ability": "Intelligence", "Amount": 1 }
                            ],
                            "Senses": [
                                { "Type": "Darkvision", "RangeFeet": 60 }
                            ]
                        }
                    ],
                    "MovementSpeeds": [
                        { "Mode": "Swim", "SpeedFeet": 30 }
                    ],
                    "DamageResponses": [
                        { "DamageType": "fire", "ResponseType": "Resistance" }
                    ],
                    "ConditionImmunities": [
                        { "Condition": "Poisoned" }
                    ]
                }
            ],
            "Classes": [
                {
                    "Id": "class.test",
                    "Name": "Test Class",
                    "HitDie": "D8",
                    "SavingThrowProficiencies": [ "Wisdom", "Charisma" ],
                    "SpellcastingAbility": "Wisdom",
                    "SpellSlotsByLevel": { "1": 2 },
                    "FeaturesByLevel": { "1": [ "feature.test" ] }
                }
            ],
            "Backgrounds": [
                {
                    "Id": "background.test",
                    "Name": "Test Background",
                    "SkillProficiencies": [ "skill.test" ]
                }
            ],
            "Skills": [
                { "Id": "skill.test", "Name": "Test Skill", "Ability": "Wisdom" }
            ],
            "Armors": [
                {
                    "Id": "armor.test",
                    "Name": "Test Armor",
                    "Category": "Medium",
                    "BaseArmorClass": 14,
                    "AddsDexterityModifier": true,
                    "MaximumDexterityModifier": 2
                }
            ],
            "Weapons": [
                {
                    "Id": "weapon.test",
                    "Name": "Test Weapon",
                    "Category": "Martial",
                    "AttackKind": "Melee",
                    "Damage": { "Count": 1, "Die": "D8" },
                    "VersatileDamage": { "Count": 1, "Die": "D10" },
                    "DamageType": "slashing",
                    "Properties": [ "weapon_property.versatile" ]
                }
            ],
            "Monsters": [
                {
                    "Id": "monster.test",
                    "Name": "Test Monster",
                    "MaximumHitPoints": 12,
                    "ArmorClass": 13,
                    "MovementSpeedFeet": 30,
                    "ZeroHitPointPolicy": "Defeated",
                    "AbilityModifiers": [
                        { "Ability": "Strength", "Modifier": 2 },
                        { "Ability": "Dexterity", "Modifier": 1 },
                        { "Ability": "Constitution", "Modifier": 1 },
                        { "Ability": "Intelligence", "Modifier": 0 },
                        { "Ability": "Wisdom", "Modifier": 0 },
                        { "Ability": "Charisma", "Modifier": 0 }
                    ],
                    "ProficiencyBonus": 2,
                    "ExperienceValue": 25,
                    "Weapons": [
                        {
                            "WeaponId": "weapon.test",
                            "AmmunitionItemId": "item.test",
                            "AmmunitionQuantity": 5
                        }
                    ]
                }
            ],
            "Spells": [
                {
                    "Id": "spell.test",
                    "Name": "Test Spell",
                    "Cost": "Slot",
                    "Level": 1,
                    "CastingTime": "Action",
                    "RangeKind": "Ranged",
                    "RangeFeet": 60,
                    "Targets": "Enemies",
                    "Resolution": "SavingThrow",
                    "SaveAbility": "Dexterity",
                    "SaveOutcome": "HalvesDamage",
                    "Effects": [
                        {
                            "Kind": "Damage",
                            "Dice": { "Count": 2, "Die": "D6" },
                            "DamageType": "fire"
                        }
                    ]
                }
            ],
            "Effects": [
                {
                    "Id": "effect.test",
                    "Name": "Test Effect",
                    "Contributions": [
                        {
                            "Target": "AttackRoll",
                            "Dice": { "Count": 1, "Die": "D4" }
                        }
                    ]
                }
            ],
            "Features": [
                {
                    "Id": "feature.test",
                    "Name": "Test Feature",
                    "Contributions": [
                        {
                            "Target": "DamageRoll",
                            "FlatBonus": 2,
                            "Conditions": [ "FinesseOrRangedWeapon" ]
                        }
                    ]
                }
            ],
            "EquipmentItems": [
                {
                    "Id": "item.test",
                    "Name": "Test Item",
                    "WeightPounds": 1.5,
                    "CostInCopperPieces": 10,
                    "Tags": [ "adventuring_gear" ]
                }
            ]
        }
        """;

    [Fact]
    public void Parse_SinglePackWithFullContent_MapsEveryField()
    {
        RulesetDefinition ruleset = RulesetPackLoader.Parse(
            [BasePackWithFullContent]);

        Assert.Equal("ruleset.test", ruleset.Id);
        Assert.Equal("Test Ruleset", ruleset.Name);

        RaceDefinition race = Assert.Single(ruleset.Races);
        Assert.Equal(CharacterSize.Small, race.Size);
        Assert.Equal(25, race.BaseSpeedFeet);
        Assert.Equal(
            Ability.Dexterity,
            Assert.Single(race.AbilityScoreIncreases).Ability);
        Assert.Equal(
            DamageResponseType.Resistance,
            Assert.Single(race.DamageResponses).ResponseType);
        Assert.Equal(
            ConditionType.Poisoned,
            Assert.Single(race.ConditionImmunities).Condition);
        Assert.Equal(
            MovementMode.Swim,
            Assert.Single(race.MovementSpeeds).Mode);

        SubraceDefinition subrace = Assert.Single(race.Subraces);
        Assert.Equal(
            Ability.Intelligence,
            Assert.Single(subrace.AbilityScoreIncreases).Ability);
        Assert.Equal(
            SenseType.Darkvision,
            Assert.Single(subrace.Senses).Type);

        ClassDefinition characterClass = Assert.Single(ruleset.Classes);
        Assert.Equal(DieType.D8, characterClass.HitDie);
        Assert.Equal(
            [Ability.Wisdom, Ability.Charisma],
            characterClass.SavingThrowProficiencies);
        Assert.Equal(Ability.Wisdom, characterClass.SpellcastingAbility);
        Assert.Equal(2, characterClass.SpellSlotsByLevel[1]);
        Assert.Equal(
            "feature.test",
            Assert.Single(characterClass.FeaturesByLevel[1]));

        ArmorDefinition armor = Assert.Single(ruleset.Armors);
        Assert.Equal(ArmorCategory.Medium, armor.Category);
        Assert.True(armor.AddsDexterityModifier);

        WeaponDefinition weapon = Assert.Single(ruleset.Weapons);
        Assert.Equal(WeaponCategory.Martial, weapon.Category);
        Assert.Equal(WeaponAttackKind.Melee, weapon.AttackKind);
        Assert.Equal(DieType.D8, weapon.Damage.Die);
        Assert.NotNull(weapon.VersatileDamage);
        Assert.Equal(DieType.D10, weapon.VersatileDamage.Die);

        MonsterDefinition monster = Assert.Single(ruleset.Monsters);
        Assert.Equal(12, monster.MaximumHitPoints);
        Assert.Equal(13, monster.ArmorClass);
        Assert.Equal(25, monster.ExperienceValue);
        Assert.Equal(
            CombatantZeroHitPointPolicy.Defeated,
            monster.ZeroHitPointPolicy);
        Assert.Equal(6, monster.AbilityModifiers.Count);
        Assert.Contains(
            monster.AbilityModifiers,
            modifier => modifier.Ability == Ability.Strength
                && modifier.Modifier == 2);
        MonsterWeaponDefinition monsterWeapon =
            Assert.Single(monster.Weapons);
        Assert.Equal("weapon.test", monsterWeapon.WeaponId);
        Assert.Equal("item.test", monsterWeapon.AmmunitionItemId);
        Assert.Equal(5, monsterWeapon.AmmunitionQuantity);

        SpellDefinition spell = Assert.Single(ruleset.Spells);
        Assert.Equal(SpellCostKind.Slot, spell.Cost);
        Assert.Equal(SpellRangeKind.Ranged, spell.RangeKind);
        Assert.Equal(SpellTargetDisposition.Enemies, spell.Targets);
        Assert.Equal(SpellResolutionKind.SavingThrow, spell.Resolution);
        Assert.Equal(Ability.Dexterity, spell.SaveAbility);
        Assert.Equal(SpellSaveOutcome.HalvesDamage, spell.SaveOutcome);
        Assert.Equal(
            SpellEffectKind.Damage,
            Assert.Single(spell.Effects).Kind);

        EffectDefinition effect = Assert.Single(ruleset.Effects);
        Assert.Equal(
            RollContributionTarget.AttackRoll,
            Assert.Single(effect.Contributions).Target);

        FeatureDefinition feature = Assert.Single(ruleset.Features);
        Assert.Equal(
            RollContributionCondition.FinesseOrRangedWeapon,
            Assert.Single(Assert.Single(feature.Contributions).Conditions));

        EquipmentItemDefinition item = Assert.Single(ruleset.EquipmentItems);
        Assert.Equal(1.5m, item.WeightPounds);
        Assert.Equal(10, item.CostInCopperPieces);
    }

    [Fact]
    public void Parse_BaseAndAdditivePack_UnionsContentAcrossPacks()
    {
        const string additivePack = """
            {
                "FormatVersion": 1,
                "Weapons": [
                    {
                        "Id": "weapon.additive",
                        "Name": "Additive Weapon",
                        "Category": "Simple",
                        "AttackKind": "Ranged",
                        "Damage": { "Count": 1, "Die": "D4" },
                        "DamageType": "piercing"
                    }
                ]
            }
            """;

        RulesetDefinition ruleset = RulesetPackLoader.Parse(
            [BasePackWithFullContent, additivePack]);

        Assert.Equal("ruleset.test", ruleset.Id);
        Assert.Equal(2, ruleset.Weapons.Count);
        Assert.Contains(
            ruleset.Weapons,
            weapon => weapon.Id == "weapon.test");
        Assert.Contains(
            ruleset.Weapons,
            weapon => weapon.Id == "weapon.additive");
    }

    [Fact]
    public void Parse_WithNoPackDeclaringIdentity_Throws()
    {
        const string additiveOnly = """
            { "FormatVersion": 1 }
            """;

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => RulesetPackLoader.Parse([additiveOnly]));

        Assert.Contains("Exactly one pack", exception.Message);
    }

    [Fact]
    public void Parse_WithTwoPacksDeclaringIdentity_Throws()
    {
        const string secondBasePack = """
            { "FormatVersion": 1, "Id": "ruleset.other", "Name": "Other" }
            """;

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => RulesetPackLoader.Parse(
                [BasePackWithFullContent, secondBasePack]));

        Assert.Contains("Exactly one pack", exception.Message);
    }

    [Fact]
    public void Parse_WithBasePackMissingName_Throws()
    {
        const string idOnlyPack = """
            { "FormatVersion": 1, "Id": "ruleset.test" }
            """;

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => RulesetPackLoader.Parse([idOnlyPack]));

        Assert.Contains("must supply both Id and Name", exception.Message);
    }

    [Fact]
    public void DeserializePack_WithUnsupportedFormatVersion_Throws()
    {
        const string futureVersionPack = """
            { "FormatVersion": 2, "Id": "ruleset.test", "Name": "Test" }
            """;

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => RulesetPackLoader.Parse([futureVersionPack]));

        Assert.Contains("format version", exception.Message);
    }

    [Fact]
    public void Load_WithDuplicateWeaponIdAcrossPacks_ThrowsWithCollisionMessage()
    {
        const string duplicatingPack = """
            {
                "FormatVersion": 1,
                "Weapons": [
                    {
                        "Id": "weapon.test",
                        "Name": "Duplicate Weapon",
                        "Category": "Simple",
                        "AttackKind": "Ranged",
                        "Damage": { "Count": 1, "Die": "D4" },
                        "DamageType": "piercing"
                    }
                ]
            }
            """;

        string basePath = Path.GetTempFileName();
        string additivePath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(basePath, BasePackWithFullContent);
            File.WriteAllText(additivePath, duplicatingPack);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => RulesetPackLoader.Load([basePath, additivePath]));

            Assert.Contains(
                "ruleset.weapons.duplicate_id",
                exception.Message);
        }
        finally
        {
            File.Delete(basePath);
            File.Delete(additivePath);
        }
    }

    [Fact]
    public void Load_WithValidPacks_ProducesLoadableRuleset()
    {
        string basePath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(basePath, BasePackWithFullContent);

            ValidatedRuleset ruleset = RulesetPackLoader.Load([basePath]);

            Assert.Equal("ruleset.test", ruleset.Definition.Id);
            Assert.True(
                ruleset.TryGetBackground(
                    "background.test",
                    out BackgroundDefinition? background));
            Assert.NotNull(background);
        }
        finally
        {
            File.Delete(basePath);
        }
    }
}
