using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverEquippedWeightTests
{
    [Fact]
    public void Resolve_WithNoEquippedGear_SetsEquippedWeightToZero()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(0m, snapshot.EquippedWeightPounds);
    }

    [Fact]
    public void Resolve_WithEquippedArmor_IncludesArmorWeight()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.leather"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(10m, snapshot.EquippedWeightPounds);
    }

    [Fact]
    public void Resolve_WithEquippedArmorAndShield_IncludesBothArmorWeights()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.leather",
            EquippedShieldId = "armor.shield"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(16m, snapshot.EquippedWeightPounds);
    }

    [Fact]
    public void Resolve_WithEquippedWeapons_IncludesWeaponWeights()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedWeaponIds =
            [
                "weapon.longsword",
                "weapon.shortbow"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(5m, snapshot.EquippedWeightPounds);
    }

    [Fact]
    public void Resolve_WithArmorShieldAndWeapons_IncludesAllEquippedWeights()
    {
        RulesetDefinition ruleset = CreateEquipmentRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedArmorId = "armor.leather",
            EquippedShieldId = "armor.shield",
            EquippedWeaponIds =
            [
                "weapon.longsword",
                "weapon.shortbow"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(21m, snapshot.EquippedWeightPounds);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Equipped Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 10,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreateEquipmentRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.equipment_weight",
            Name = "Equipment Weight Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.test",
                    Name = "Test Race",
                    BaseSpeedFeet = 30
                }
            ],
            Armors =
            [
                CreateLeatherArmor(),
                CreateShield()
            ],
            Weapons =
            [
                CreateLongsword(),
                CreateShortbow()
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

    private static WeaponDefinition CreateLongsword()
    {
        return new WeaponDefinition
        {
            Id = "weapon.longsword",
            Name = "Longsword",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            DamageType = "damage.slashing",
            WeightPounds = 3m
        };
    }

    private static WeaponDefinition CreateShortbow()
    {
        return new WeaponDefinition
        {
            Id = "weapon.shortbow",
            Name = "Shortbow",
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Ranged,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            DamageType = "damage.piercing",
            NormalRangeFeet = 80,
            LongRangeFeet = 320,
            WeightPounds = 2m
        };
    }
}
