using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Tests.Builders;

namespace FiveEGoldBox.Core.Tests;

public sealed class WeaponAttackRollModeTests
{
    [Fact]
    public void Resolve_WithSmallCharacterAndHeavyWeapon_SetsAttackRollModeToDisadvantage()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.halfling",
            EquippedWeaponIds =
            [
                "weapon.greatsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = Assert.Single(snapshot.WeaponAttacks);

        Assert.True(attack.HasDisadvantage);
        Assert.Equal(D20RollMode.Disadvantage, attack.AttackRollMode);
    }

    [Fact]
    public void Resolve_WithMediumCharacterAndHeavyWeapon_SetsAttackRollModeToNormal()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            EquippedWeaponIds =
            [
                "weapon.greatsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = Assert.Single(snapshot.WeaponAttacks);

        Assert.False(attack.HasDisadvantage);
        Assert.Equal(D20RollMode.Normal, attack.AttackRollMode);
    }

    [Fact]
    public void Resolve_WithSmallCharacterAndNonHeavyWeapon_SetsAttackRollModeToNormal()
    {
        RulesetDefinition ruleset = CreateRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.halfling",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = Assert.Single(snapshot.WeaponAttacks);

        Assert.False(attack.HasDisadvantage);
        Assert.Equal(D20RollMode.Normal, attack.AttackRollMode);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Weapon Attack Roll Mode Character",
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
            Id = "ruleset.weapon_attack_roll_mode",
            Name = "Weapon Attack Roll Mode Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.human",
                    Name = "Human",
                    Size = CharacterSize.Medium,
                    BaseSpeedFeet = 30
                },
                new RaceDefinition
                {
                    Id = "race.halfling",
                    Name = "Halfling",
                    Size = CharacterSize.Small,
                    BaseSpeedFeet = 25
                }
            ],
            Weapons =
            [
                CreateGreatsword(),
                TestRulesetBuilder.Longsword()
            ]
        };
    }

    private static WeaponDefinition CreateGreatsword()
    {
        return new WeaponDefinition
        {
            Id = "weapon.greatsword",
            Name = "Greatsword",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 2,
                Die = DieType.D6
            },
            DamageType = "damage.slashing",
            Properties =
            [
                "weapon_property.heavy",
                "weapon_property.two_handed"
            ],
            WeightPounds = 6m,
            CostInCopperPieces = 5000
        };
    }

}
