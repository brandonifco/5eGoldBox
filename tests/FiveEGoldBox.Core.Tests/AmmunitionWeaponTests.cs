using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class AmmunitionWeaponTests
{
    [Fact]
    public void WeaponDefinition_CanRepresentAmmunitionItemId()
    {
        WeaponDefinition shortbow = CreateShortbow();

        Assert.Equal("weapon.shortbow", shortbow.Id);
        Assert.Equal("equipment.arrow", shortbow.AmmunitionItemId);
        Assert.Contains("weapon_property.ammunition", shortbow.Properties);
    }

    [Fact]
    public void Resolve_WithAmmunitionWeapon_CarriesAmmunitionItemIdIntoWeaponAttack()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedWeaponIds =
            [
                "weapon.shortbow"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = Assert.Single(snapshot.WeaponAttacks);

        Assert.Equal("weapon.shortbow", attack.WeaponId);
        Assert.Equal("Shortbow", attack.WeaponName);
        Assert.Equal("equipment.arrow", attack.AmmunitionItemId);
        Assert.Contains("weapon_property.ammunition", attack.Properties);
    }

    [Fact]
    public void Resolve_WithNonAmmunitionWeapon_LeavesAmmunitionItemIdNull()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = Assert.Single(snapshot.WeaponAttacks);

        Assert.Equal("weapon.longsword", attack.WeaponId);
        Assert.Null(attack.AmmunitionItemId);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Ammunition Weapon Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 10,
                [Ability.Dexterity] = 15,
                [Ability.Constitution] = 10,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 10
            }
        };
    }

    private static RulesetDefinition CreateRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.ammunition_weapon",
            Name = "Ammunition Weapon Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.test",
                    Name = "Test Race",
                    BaseSpeedFeet = 30
                }
            ],
            Weapons =
            [
                CreateShortbow(),
                CreateLongsword()
            ],
            EquipmentItems =
            [
                new EquipmentItemDefinition
                {
                    Id = "equipment.arrow",
                    Name = "Arrow",
                    WeightPounds = 0.05m,
                    CostInCopperPieces = 5
                }
            ]
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
            Properties =
            [
                "weapon_property.ammunition",
                "weapon_property.two_handed"
            ],
            NormalRangeFeet = 80,
            LongRangeFeet = 320,
            AmmunitionItemId = "equipment.arrow",
            WeightPounds = 2m,
            CostInCopperPieces = 2500
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
            VersatileDamage = new DamageDice
            {
                Count = 1,
                Die = DieType.D10
            },
            DamageType = "damage.slashing",
            Properties =
            [
                "weapon_property.versatile"
            ],
            WeightPounds = 3m,
            CostInCopperPieces = 1500
        };
    }
}