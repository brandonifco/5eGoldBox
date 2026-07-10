using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class ReachWeaponTests
{
    [Fact]
    public void WeaponDefinition_CanRepresentReach()
    {
        WeaponDefinition glaive = CreateGlaive();

        Assert.Equal("weapon.glaive", glaive.Id);
        Assert.Equal(10, glaive.ReachFeet);
        Assert.Contains("weapon_property.reach", glaive.Properties);
    }

    [Fact]
    public void Resolve_WithReachWeapon_CarriesReachIntoWeaponAttack()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            EquippedWeaponIds =
            [
                "weapon.glaive"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = Assert.Single(snapshot.WeaponAttacks);

        Assert.Equal("weapon.glaive", attack.WeaponId);
        Assert.Equal("Glaive", attack.WeaponName);
        Assert.Equal(10, attack.ReachFeet);
        Assert.Contains("weapon_property.reach", attack.Properties);
    }

    [Fact]
    public void Resolve_WithNonReachWeapon_LeavesReachNull()
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
        Assert.Null(attack.ReachFeet);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Reach Weapon Character",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 10,
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
            Id = "ruleset.reach_weapon",
            Name = "Reach Weapon Ruleset",
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
                CreateGlaive(),
                CreateLongsword()
            ]
        };
    }

    private static WeaponDefinition CreateGlaive()
    {
        return new WeaponDefinition
        {
            Id = "weapon.glaive",
            Name = "Glaive",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D10
            },
            DamageType = "damage.slashing",
            Properties =
            [
                "weapon_property.heavy",
                "weapon_property.reach",
                "weapon_property.two_handed"
            ],
            ReachFeet = 10,
            WeightPounds = 6m,
            CostInCopperPieces = 2000
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