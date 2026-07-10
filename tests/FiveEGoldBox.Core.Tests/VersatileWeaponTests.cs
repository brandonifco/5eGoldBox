using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class VersatileWeaponTests
{
    [Fact]
    public void WeaponDefinition_CanRepresentVersatileDamage()
    {
        WeaponDefinition longsword = CreateLongsword();

        Assert.Equal("weapon.longsword", longsword.Id);
        Assert.Equal(1, longsword.Damage.Count);
        Assert.Equal(DieType.D8, longsword.Damage.Die);

        Assert.NotNull(longsword.VersatileDamage);
        Assert.Equal(1, longsword.VersatileDamage.Count);
        Assert.Equal(DieType.D10, longsword.VersatileDamage.Die);

        Assert.Contains("weapon_property.versatile", longsword.Properties);
    }

    [Fact]
    public void Resolve_WithVersatileWeapon_CarriesVersatileDamageIntoWeaponAttack()
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
        Assert.Equal("Longsword", attack.WeaponName);
        Assert.Equal(1, attack.Damage.Count);
        Assert.Equal(DieType.D8, attack.Damage.Die);

        Assert.NotNull(attack.VersatileDamage);
        Assert.Equal(1, attack.VersatileDamage.Count);
        Assert.Equal(DieType.D10, attack.VersatileDamage.Die);

        Assert.Contains("weapon_property.versatile", attack.Properties);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Versatile Weapon Character",
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
            Id = "ruleset.versatile_weapon",
            Name = "Versatile Weapon Ruleset",
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
                CreateLongsword()
            ]
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