using FiveEGoldBox.ContentEditor.Services;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.ContentEditor.Tests;

/// Create/edit/delete coverage for all four content kinds, plus the
/// atomic-write invariants (a validation failure leaves the real file
/// untouched; a delete that breaks a cross-reference is rejected). Always
/// runs against a temp copy of the real committed
/// data/rulesets/campaign/core.json, never the committed file itself --
/// same discipline Phase E's Tiled importer testing already established.
public sealed class RulesetContentServiceTests
{
    // ----- Weapons -----

    [Fact]
    public void SaveWeapon_AddsANewWeapon()
    {
        WithTempPackFile(service =>
        {
            WeaponDefinition weapon = new()
            {
                Id = "weapon.test-club",
                Name = "Test Club",
                Category = WeaponCategory.Simple,
                AttackKind = WeaponAttackKind.Melee,
                Damage = new DamageDice { Count = 1, Die = DieType.D4 },
                DamageType = "damage.bludgeoning"
            };

            var result = service.SaveWeapon(weapon);

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equivalent(weapon, service.FindWeapon("weapon.test-club"));
        });
    }

    [Fact]
    public void SaveWeapon_ReplacesAnExistingWeaponWithTheSameId()
    {
        WithTempPackFile(service =>
        {
            WeaponDefinition original = service.FindWeapon("weapon.dagger")!;
            WeaponDefinition renamed = original with { Name = "Renamed Dagger" };

            var result = service.SaveWeapon(renamed);

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equal("Renamed Dagger", service.FindWeapon("weapon.dagger")!.Name);
            Assert.Equal(service.LoadWeapons().Count, service.LoadWeapons().Select(w => w.Id).Distinct().Count());
        });
    }

    [Fact]
    public void DeleteWeapon_RemovesAnUnreferencedWeapon()
    {
        WithTempPackFile(service =>
        {
            WeaponDefinition weapon = new()
            {
                Id = "weapon.test-unreferenced",
                Name = "Test Unreferenced",
                Category = WeaponCategory.Simple,
                AttackKind = WeaponAttackKind.Melee,
                Damage = new DamageDice { Count = 1, Die = DieType.D4 },
                DamageType = "damage.bludgeoning"
            };
            Assert.True(service.SaveWeapon(weapon).IsValid);

            var result = service.DeleteWeapon("weapon.test-unreferenced");

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Null(service.FindWeapon("weapon.test-unreferenced"));
        });
    }

    [Fact]
    public void DeleteWeapon_RejectsDeletingAWeaponAMonsterStillCarries()
    {
        WithTempPackFile(service =>
        {
            byte[] before = File.ReadAllBytes(service.FilePathForTesting);

            // weapon.watchtower-raider.scimitar is carried by
            // monster.watchtower-raider.marauder in the real content.
            var result = service.DeleteWeapon("weapon.watchtower-raider.scimitar");

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.Severity == FiveEGoldBox.Core.Validation.ValidationSeverity.Error);

            byte[] after = File.ReadAllBytes(service.FilePathForTesting);
            Assert.Equal(before, after);
        });
    }

    [Fact]
    public void SaveWeapon_RejectsAnUnknownWeaponPropertyAndLeavesFileUntouched()
    {
        WithTempPackFile(service =>
        {
            byte[] before = File.ReadAllBytes(service.FilePathForTesting);

            WeaponDefinition invalid = new()
            {
                Id = "weapon.test-invalid",
                Name = "Test Invalid",
                Category = WeaponCategory.Simple,
                AttackKind = WeaponAttackKind.Melee,
                Damage = new DamageDice { Count = 1, Die = DieType.D4 },
                DamageType = "damage.bludgeoning",
                Properties = ["weapon_property.not_a_real_property"]
            };

            var result = service.SaveWeapon(invalid);

            Assert.False(result.IsValid);

            byte[] after = File.ReadAllBytes(service.FilePathForTesting);
            Assert.Equal(before, after);
        });
    }

    // ----- Spells -----

    [Fact]
    public void SaveSpell_AddsANewSpellWithEffects()
    {
        WithTempPackFile(service =>
        {
            SpellDefinition spell = new()
            {
                Id = "spell.test-bolt",
                Name = "Test Bolt",
                Cost = SpellCostKind.Cantrip,
                Level = 0,
                CastingTime = SpellCastingTime.Action,
                RangeKind = SpellRangeKind.Ranged,
                RangeFeet = 30,
                Targets = SpellTargetDisposition.Enemies,
                Resolution = SpellResolutionKind.SpellAttack,
                Effects =
                [
                    new SpellEffectDefinition
                    {
                        Kind = SpellEffectKind.Damage,
                        Dice = new DamageDice { Count = 1, Die = DieType.D6 },
                        DamageType = "damage.force"
                    }
                ]
            };

            var result = service.SaveSpell(spell);

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equivalent(spell, service.FindSpell("spell.test-bolt"));
        });
    }

    [Fact]
    public void DeleteSpell_RemovesAnExistingSpell()
    {
        WithTempPackFile(service =>
        {
            var result = service.DeleteSpell("spell.healing-word");

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Null(service.FindSpell("spell.healing-word"));
        });
    }

    [Fact]
    public void SaveSpell_RejectsASavingThrowResolutionWithNoSaveAbilityAndLeavesFileUntouched()
    {
        WithTempPackFile(service =>
        {
            byte[] before = File.ReadAllBytes(service.FilePathForTesting);

            SpellDefinition invalid = new()
            {
                Id = "spell.test-invalid",
                Name = "Test Invalid",
                Cost = SpellCostKind.Cantrip,
                Level = 0,
                CastingTime = SpellCastingTime.Action,
                RangeKind = SpellRangeKind.Ranged,
                RangeFeet = 30,
                Targets = SpellTargetDisposition.Enemies,
                Resolution = SpellResolutionKind.SavingThrow,
                SaveAbility = null
            };

            var result = service.SaveSpell(invalid);

            Assert.False(result.IsValid);

            byte[] after = File.ReadAllBytes(service.FilePathForTesting);
            Assert.Equal(before, after);
        });
    }

    // ----- Equipment items -----

    [Fact]
    public void SaveEquipmentItem_AddsANewItem()
    {
        WithTempPackFile(service =>
        {
            EquipmentItemDefinition item = new()
            {
                Id = "item.test-torch",
                Name = "Test Torch",
                WeightPounds = 1m,
                Tags = ["tag.light-source"]
            };

            var result = service.SaveEquipmentItem(item);

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equivalent(item, service.FindEquipmentItem("item.test-torch"));
        });
    }

    [Fact]
    public void DeleteEquipmentItem_RejectsDeletingAnItemAWeaponStillReferencesAsAmmunition()
    {
        WithTempPackFile(service =>
        {
            byte[] before = File.ReadAllBytes(service.FilePathForTesting);

            // item.arrow is referenced by weapon.longbow's AmmunitionItemId.
            var result = service.DeleteEquipmentItem("item.arrow");

            Assert.False(result.IsValid);

            byte[] after = File.ReadAllBytes(service.FilePathForTesting);
            Assert.Equal(before, after);
        });
    }

    // ----- Monsters -----

    [Fact]
    public void SaveMonster_AddsANewMonsterWithAllSixAbilityModifiers()
    {
        WithTempPackFile(service =>
        {
            MonsterDefinition monster = new()
            {
                Id = "monster.test-goblin",
                Name = "Test Goblin",
                MaximumHitPoints = 7,
                ArmorClass = 15,
                MovementSpeedFeet = 30,
                ZeroHitPointPolicy = CombatantZeroHitPointPolicy.Defeated,
                AbilityModifiers =
                [
                    new MonsterAbilityModifier { Ability = Ability.Strength, Modifier = 0 },
                    new MonsterAbilityModifier { Ability = Ability.Dexterity, Modifier = 2 },
                    new MonsterAbilityModifier { Ability = Ability.Constitution, Modifier = 0 },
                    new MonsterAbilityModifier { Ability = Ability.Intelligence, Modifier = -1 },
                    new MonsterAbilityModifier { Ability = Ability.Wisdom, Modifier = 0 },
                    new MonsterAbilityModifier { Ability = Ability.Charisma, Modifier = -1 }
                ],
                ProficiencyBonus = 2,
                Weapons = [new MonsterWeaponDefinition { WeaponId = "weapon.dagger" }]
            };

            var result = service.SaveMonster(monster);

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equivalent(monster, service.FindMonster("monster.test-goblin"));
        });
    }

    [Fact]
    public void SaveMonster_RejectsAMissingAbilityModifierAndLeavesFileUntouched()
    {
        WithTempPackFile(service =>
        {
            byte[] before = File.ReadAllBytes(service.FilePathForTesting);

            MonsterDefinition incomplete = new()
            {
                Id = "monster.test-incomplete",
                Name = "Test Incomplete",
                MaximumHitPoints = 7,
                ArmorClass = 15,
                MovementSpeedFeet = 30,
                ZeroHitPointPolicy = CombatantZeroHitPointPolicy.Defeated,
                AbilityModifiers =
                [
                    new MonsterAbilityModifier { Ability = Ability.Strength, Modifier = 0 }

                    // Missing the other five abilities -- required to be a
                    // complete set of exactly six.
                ],
                ProficiencyBonus = 2,
                Weapons = [new MonsterWeaponDefinition { WeaponId = "weapon.dagger" }]
            };

            var result = service.SaveMonster(incomplete);

            Assert.False(result.IsValid);

            byte[] after = File.ReadAllBytes(service.FilePathForTesting);
            Assert.Equal(before, after);
        });
    }

    [Fact]
    public void SaveMonster_RejectsAnUnknownWeaponReferenceAndLeavesFileUntouched()
    {
        WithTempPackFile(service =>
        {
            byte[] before = File.ReadAllBytes(service.FilePathForTesting);

            MonsterDefinition monster = new()
            {
                Id = "monster.test-unknown-weapon",
                Name = "Test Unknown Weapon",
                MaximumHitPoints = 7,
                ArmorClass = 15,
                MovementSpeedFeet = 30,
                ZeroHitPointPolicy = CombatantZeroHitPointPolicy.Defeated,
                AbilityModifiers =
                [
                    new MonsterAbilityModifier { Ability = Ability.Strength, Modifier = 0 },
                    new MonsterAbilityModifier { Ability = Ability.Dexterity, Modifier = 0 },
                    new MonsterAbilityModifier { Ability = Ability.Constitution, Modifier = 0 },
                    new MonsterAbilityModifier { Ability = Ability.Intelligence, Modifier = 0 },
                    new MonsterAbilityModifier { Ability = Ability.Wisdom, Modifier = 0 },
                    new MonsterAbilityModifier { Ability = Ability.Charisma, Modifier = 0 }
                ],
                ProficiencyBonus = 2,
                Weapons = [new MonsterWeaponDefinition { WeaponId = "weapon.does-not-exist" }]
            };

            var result = service.SaveMonster(monster);

            Assert.False(result.IsValid);

            byte[] after = File.ReadAllBytes(service.FilePathForTesting);
            Assert.Equal(before, after);
        });
    }

    [Fact]
    public void DeleteMonster_RemovesAnExistingMonster()
    {
        WithTempPackFile(service =>
        {
            var result = service.DeleteMonster("monster.mill-rat");

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Null(service.FindMonster("monster.mill-rat"));
        });
    }

    // ----- Cross-reference reads -----

    [Fact]
    public void LoadEffects_ReturnsTheFilesCurrentEffects()
    {
        WithTempPackFile(service =>
        {
            Assert.Contains(service.LoadEffects(), effect => effect.Id == "effect.bless");
        });
    }

    private static void WithTempPackFile(
        Action<RulesetContentService> action)
    {
        string tempFile = NoOpSaveFormattingTests.CopyRealRulesetPackToTempFile();

        try
        {
            action(new RulesetContentService(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static string DescribeIssues(
        FiveEGoldBox.Core.Validation.ValidationResult result)
    {
        return string.Join(
            "\n",
            result.Issues.Select(issue => $"{issue.Severity} {issue.Code}: {issue.Message}"));
    }
}
