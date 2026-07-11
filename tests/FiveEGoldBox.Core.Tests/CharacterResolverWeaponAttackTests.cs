using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverWeaponAttackTests
{
    [Fact]
    public void Resolve_WithProficientMeleeWeapon_CalculatesStrengthAttackAndDamage()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = GetWeaponAttack(snapshot, "weapon.longsword");

        Assert.Equal("Longsword", attack.WeaponName);
        Assert.Equal(WeaponCategory.Martial, attack.Category);
        Assert.Equal(WeaponAttackKind.Melee, attack.AttackKind);
        Assert.Equal(Ability.Strength, attack.AttackAbility);
        Assert.True(attack.IsProficient);
        Assert.Equal(3, attack.AbilityModifier);
        Assert.Equal(2, attack.ProficiencyBonus);
        Assert.Equal(5, attack.AttackBonus);
        Assert.Equal(1, attack.Damage.Count);
        Assert.Equal(DieType.D8, attack.Damage.Die);
        Assert.Equal("damage.slashing", attack.DamageType);
        Assert.Equal(3, attack.DamageBonus);
    }

    [Fact]
    public void Resolve_WithProficientRangedWeapon_CalculatesDexterityAttackAndDamage()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedWeaponIds =
            [
                "weapon.shortbow"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = GetWeaponAttack(snapshot, "weapon.shortbow");

        Assert.Equal("Shortbow", attack.WeaponName);
        Assert.Equal(WeaponCategory.Simple, attack.Category);
        Assert.Equal(WeaponAttackKind.Ranged, attack.AttackKind);
        Assert.Equal(Ability.Dexterity, attack.AttackAbility);
        Assert.True(attack.IsProficient);
        Assert.Equal(2, attack.AbilityModifier);
        Assert.Equal(2, attack.ProficiencyBonus);
        Assert.Equal(4, attack.AttackBonus);
        Assert.Equal(1, attack.Damage.Count);
        Assert.Equal(DieType.D6, attack.Damage.Die);
        Assert.Equal("damage.piercing", attack.DamageType);
        Assert.Equal(2, attack.DamageBonus);
        Assert.Equal(80, attack.NormalRangeFeet);
        Assert.Equal(320, attack.LongRangeFeet);
    }

    [Fact]
    public void Resolve_WithNonProficientWeapon_DoesNotAddProficiencyBonus()
    {
        RulesetDefinition ruleset = CreateSimpleOnlyRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.simple_only",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = GetWeaponAttack(snapshot, "weapon.longsword");

        Assert.False(attack.IsProficient);
        Assert.Equal(3, attack.AbilityModifier);
        Assert.Equal(0, attack.ProficiencyBonus);
        Assert.Equal(3, attack.AttackBonus);
        Assert.Equal(3, attack.DamageBonus);
    }

    [Fact]
    public void Resolve_WithSpecificWeaponProficiency_AddsProficiencyBonus()
    {
        RulesetDefinition ruleset = CreateSpecificWeaponProficiencyRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.longsword_only",
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = GetWeaponAttack(snapshot, "weapon.longsword");

        Assert.True(attack.IsProficient);
        Assert.Equal(5, attack.AttackBonus);
    }

    [Fact]
    public void Resolve_WithFinesseWeapon_UsesBetterOfStrengthOrDexterity()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateHighDexterityDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter",
            EquippedWeaponIds =
            [
                "weapon.rapier"
            ]
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = GetWeaponAttack(snapshot, "weapon.rapier");

        Assert.Equal(Ability.Dexterity, attack.AttackAbility);
        Assert.Equal(4, attack.AbilityModifier);
        Assert.Equal(2, attack.ProficiencyBonus);
        Assert.Equal(6, attack.AttackBonus);
        Assert.Equal(4, attack.DamageBonus);
    }

    [Fact]
    public void Resolve_WithoutSelectedClass_CalculatesNonProficientWeaponAttack()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.test",
            ClassId = null,
            EquippedWeaponIds =
            [
                "weapon.longsword"
            ]
        };

        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.weapons_only",
            Name = "Weapons Only",
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

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        WeaponAttack attack = GetWeaponAttack(snapshot, "weapon.longsword");

        Assert.False(attack.IsProficient);
        Assert.Equal(2, attack.AbilityModifier);
        Assert.Equal(0, attack.ProficiencyBonus);
        Assert.Equal(2, attack.AttackBonus);
    }

    private static WeaponAttack GetWeaponAttack(CharacterSnapshot snapshot, string weaponId)
    {
        return Assert.Single(
            snapshot.WeaponAttacks,
            attack => attack.WeaponId == weaponId);
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

    private static CharacterDraft CreateHighDexterityDraft()
    {
        return new CharacterDraft
        {
            Name = "High Dexterity Fighter",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 12,
                [Ability.Dexterity] = 17,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 10,
                [Ability.Wisdom] = 8,
                [Ability.Charisma] = 14
            }
        };
    }

    private static RulesetDefinition CreateTestRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                CreateFighterClass()
            ],
            Weapons =
            [
                CreateLongsword(),
                CreateShortbow(),
                CreateRapier()
            ]
        };
    }

    private static RulesetDefinition CreateSimpleOnlyRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.simple_only",
            Name = "Simple Only Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.simple_only",
                    Name = "Simple Only",
                    HitDie = DieType.D8,
                    WeaponProficiencies =
                    [
                        "weapon.simple"
                    ]
                }
            ],
            Weapons =
            [
                CreateLongsword()
            ]
        };
    }

    private static RulesetDefinition CreateSpecificWeaponProficiencyRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.longsword_only",
            Name = "Longsword Only Ruleset",
            Races =
            [
                CreateHumanRace()
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.longsword_only",
                    Name = "Longsword Only",
                    HitDie = DieType.D8,
                    WeaponProficiencies =
                    [
                        "weapon.longsword"
                    ]
                }
            ],
            Weapons =
            [
                CreateLongsword()
            ]
        };
    }

    private static RaceDefinition CreateHumanRace()
    {
        return new RaceDefinition
        {
            Id = "race.human",
            Name = "Human",
            BaseSpeedFeet = 30,
            AbilityScoreIncreases =
            [
                new AbilityScoreIncrease(Ability.Strength, 1),
                new AbilityScoreIncrease(Ability.Dexterity, 1),
                new AbilityScoreIncrease(Ability.Constitution, 1),
                new AbilityScoreIncrease(Ability.Intelligence, 1),
                new AbilityScoreIncrease(Ability.Wisdom, 1),
                new AbilityScoreIncrease(Ability.Charisma, 1)
            ],
            Languages =
            [
                "language.common"
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
            SavingThrowProficiencies =
            [
                Ability.Strength,
                Ability.Constitution
            ],
            WeaponProficiencies =
            [
                "weapon.simple",
                "weapon.martial"
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
            Properties =
            [
                "weapon_property.ammunition",
                "weapon_property.two_handed"
            ],
            NormalRangeFeet = 80,
            LongRangeFeet = 320,
            WeightPounds = 2m
        };
    }

    private static WeaponDefinition CreateRapier()
    {
        return new WeaponDefinition
        {
            Id = "weapon.rapier",
            Name = "Rapier",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            DamageType = "damage.piercing",
            Properties =
            [
                "weapon_property.finesse"
            ],
            WeightPounds = 2m
        };
    }
}
