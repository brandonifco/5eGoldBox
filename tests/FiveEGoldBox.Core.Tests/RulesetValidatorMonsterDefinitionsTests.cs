using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

/// Content checks for the bestiary. A monster's own stat-block concerns
/// (hit points, armor class, ability modifiers, weapons) moved here from the
/// scenario-side validator when MonsterDefinition was extracted -- these are
/// the same checks a scenario-authored CombatantDefinition used to carry,
/// applied once against the ruleset instead of once per encounter placement.
public sealed class RulesetValidatorMonsterDefinitionsTests
{
    [Fact]
    public void Validate_WithACoherentMonster_ReturnsSuccess()
    {
        Assert.True(Validate(Valid()).IsValid);
    }

    [Fact]
    public void Validate_WithANamelessMonster_ReturnsError()
    {
        AssertRejects(
            Valid() with { Name = " " },
            "ruleset.monsters.name.required");
    }

    [Fact]
    public void Validate_WithABlankMonsterId_ReturnsError()
    {
        AssertRejects(
            Valid() with { Id = " " },
            "ruleset.monsters.id.required");
    }

    [Fact]
    public void Validate_WithTwoMonstersSharingAnId_ReturnsError()
    {
        ValidationResult result = Validate(Valid(), Valid());

        Assert.Contains(
            result.Issues,
            issue => issue.Code == "ruleset.monsters.duplicate_id");
    }

    [Fact]
    public void Validate_WithAnUndefinedAbility_ReturnsError()
    {
        AssertRejects(
            Valid() with
            {
                AbilityModifiers =
                [
                    new MonsterAbilityModifier
                    {
                        Ability = (Ability)99,
                        Modifier = 0
                    }
                ]
            },
            "ruleset.monsters.ability_undefined");
    }

    [Fact]
    public void Validate_WithADuplicateAbility_ReturnsError()
    {
        AssertRejects(
            Valid() with
            {
                AbilityModifiers =
                [
                    .. Valid().AbilityModifiers,
                    new MonsterAbilityModifier
                    {
                        Ability = Ability.Strength,
                        Modifier = 1
                    }
                ]
            },
            "ruleset.monsters.ability_duplicate");
    }

    [Fact]
    public void Validate_WithAMissingAbility_ReturnsError()
    {
        AssertRejects(
            Valid() with
            {
                AbilityModifiers = Valid().AbilityModifiers
                    .Where(modifier => modifier.Ability != Ability.Charisma)
                    .ToArray()
            },
            "ruleset.monsters.ability_missing");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveHitPoints_ReturnsError(
        int maximumHitPoints)
    {
        AssertRejects(
            Valid() with { MaximumHitPoints = maximumHitPoints },
            "ruleset.monsters.hit_points");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveArmorClass_ReturnsError(
        int armorClass)
    {
        AssertRejects(
            Valid() with { ArmorClass = armorClass },
            "ruleset.monsters.armor_class");
    }

    [Fact]
    public void Validate_WithNegativeMovementSpeed_ReturnsError()
    {
        AssertRejects(
            Valid() with { MovementSpeedFeet = -1 },
            "ruleset.monsters.movement_speed");
    }

    [Fact]
    public void Validate_WithNegativeProficiencyBonus_ReturnsError()
    {
        AssertRejects(
            Valid() with { ProficiencyBonus = -1 },
            "ruleset.monsters.proficiency_negative");
    }

    [Fact]
    public void Validate_WithAnUndefinedZeroHitPointPolicy_ReturnsError()
    {
        AssertRejects(
            Valid() with { ZeroHitPointPolicy = (CombatantZeroHitPointPolicy)99 },
            "ruleset.monsters.zero_hit_point_policy");
    }

    [Fact]
    public void Validate_WithNoWeapon_ReturnsError()
    {
        AssertRejects(
            Valid() with { Weapons = Array.Empty<MonsterWeaponDefinition>() },
            "ruleset.monsters.no_weapon");
    }

    [Fact]
    public void Validate_WithDuplicateWeaponIds_ReturnsError()
    {
        AssertRejects(
            Valid() with
            {
                Weapons =
                [
                    new MonsterWeaponDefinition { WeaponId = "weapon.test" },
                    new MonsterWeaponDefinition { WeaponId = "weapon.test" }
                ]
            },
            "ruleset.monsters.duplicate_weapon");
    }

    [Fact]
    public void Validate_WithABlankWeaponId_ReturnsError()
    {
        AssertRejects(
            Valid() with
            {
                Weapons = [new MonsterWeaponDefinition { WeaponId = " " }]
            },
            "ruleset.monsters.weapon_id_required");
    }

    [Fact]
    public void Validate_WithAnAmmunitionItemButNoQuantity_ReturnsError()
    {
        AssertRejects(
            Valid() with
            {
                Weapons =
                [
                    new MonsterWeaponDefinition
                    {
                        WeaponId = "weapon.test",
                        AmmunitionItemId = "item.arrow",
                        AmmunitionQuantity = null
                    }
                ]
            },
            "ruleset.monsters.ammunition_incomplete");
    }

    [Fact]
    public void Validate_WithAnAmmunitionQuantityButNoItem_ReturnsError()
    {
        AssertRejects(
            Valid() with
            {
                Weapons =
                [
                    new MonsterWeaponDefinition
                    {
                        WeaponId = "weapon.test",
                        AmmunitionItemId = null,
                        AmmunitionQuantity = 10
                    }
                ]
            },
            "ruleset.monsters.ammunition_incomplete");
    }

    [Fact]
    public void Validate_WithNegativeAmmunitionQuantity_ReturnsError()
    {
        AssertRejects(
            Valid() with
            {
                Weapons =
                [
                    new MonsterWeaponDefinition
                    {
                        WeaponId = "weapon.test",
                        AmmunitionItemId = "item.arrow",
                        AmmunitionQuantity = -1
                    }
                ]
            },
            "ruleset.monsters.ammunition_negative");
    }

    /// The cross-pack reference check lives in RulesetValidatorReferences.cs
    /// rather than RulesetValidatorMonsterDefinitions.cs, but it is exactly as
    /// much a content check on a monster as the rest of this file.
    [Fact]
    public void Validate_WithAnUnknownWeaponReference_ReturnsError()
    {
        ValidationResult result = RulesetValidator.Validate(new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Monsters =
            [
                Valid() with
                {
                    Weapons =
                    [
                        new MonsterWeaponDefinition
                        {
                            WeaponId = "weapon.unknown"
                        }
                    ]
                }
            ]
        });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "ruleset.monsters.weapons.unknown_weapon"
                && issue.Message.Contains("weapon.unknown"));
    }

    private static MonsterDefinition Valid()
    {
        return new MonsterDefinition
        {
            Id = "monster.test",
            Name = "Test Monster",
            MaximumHitPoints = 10,
            ArmorClass = 12,
            MovementSpeedFeet = 30,
            ZeroHitPointPolicy = CombatantZeroHitPointPolicy.Defeated,
            AbilityModifiers = Enum.GetValues<Ability>()
                .Select(ability => new MonsterAbilityModifier
                {
                    Ability = ability,
                    Modifier = 0
                })
                .ToArray(),
            ProficiencyBonus = 2,
            Weapons = [new MonsterWeaponDefinition { WeaponId = "weapon.test" }]
        };
    }

    private static WeaponDefinition TestWeapon()
    {
        return new WeaponDefinition
        {
            Id = "weapon.test",
            Name = "Test Weapon",
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice { Count = 1, Die = DieType.D6 },
            DamageType = "damage.test"
        };
    }

    private static void AssertRejects(
        MonsterDefinition monster,
        string expectedCode)
    {
        ValidationResult result = Validate(monster);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == expectedCode);
    }

    private static ValidationResult Validate(
        params MonsterDefinition[] monsters)
    {
        return RulesetValidator.Validate(new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Weapons = [TestWeapon()],
            Monsters = monsters
        });
    }
}
