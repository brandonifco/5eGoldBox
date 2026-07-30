using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddMonsterDefinitionIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<MonsterDefinition> monsters)
    {
        foreach (MonsterDefinition monster in monsters)
        {
            AddMonsterAbilityModifierIssues(issues, monster);

            if (monster.MaximumHitPoints <= 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.monsters.hit_points",
                    $"Ruleset monster '{monster.Id}' must have positive maximum hit points."));
            }

            if (monster.ArmorClass <= 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.monsters.armor_class",
                    $"Ruleset monster '{monster.Id}' must have a positive armor class."));
            }

            if (monster.MovementSpeedFeet < 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.monsters.movement_speed",
                    $"Ruleset monster '{monster.Id}' must not have negative movement."));
            }

            if (monster.ProficiencyBonus < 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.monsters.proficiency_negative",
                    $"Ruleset monster '{monster.Id}' has a negative proficiency bonus."));
            }

            if (!Enum.IsDefined(monster.ZeroHitPointPolicy))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.monsters.zero_hit_point_policy",
                    $"Ruleset monster '{monster.Id}' has an unsupported zero-hit-point policy."));
            }

            AddMonsterWeaponIssues(issues, monster);
        }
    }

    /// Every ability, exactly once. A missing modifier would only surface when
    /// something asked this monster for that saving throw, which could be most
    /// of the way through an adventure.
    private static void AddMonsterAbilityModifierIssues(
        List<ValidationIssue> issues,
        MonsterDefinition monster)
    {
        HashSet<Ability> declared = [];

        foreach (MonsterAbilityModifier modifier in monster.AbilityModifiers)
        {
            if (!Enum.IsDefined(modifier.Ability))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.monsters.ability_undefined",
                    $"Ruleset monster '{monster.Id}' declares a modifier for undefined ability '{(int)modifier.Ability}'."));
                continue;
            }

            if (!declared.Add(modifier.Ability))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.monsters.ability_duplicate",
                    $"Ruleset monster '{monster.Id}' declares ability '{modifier.Ability}' more than once."));
            }
        }

        foreach (Ability ability in Enum.GetValues<Ability>()
            .Where(ability => !declared.Contains(ability)))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.monsters.ability_missing",
                $"Ruleset monster '{monster.Id}' declares no modifier for ability '{ability}'."));
        }
    }

    private static void AddMonsterWeaponIssues(
        List<ValidationIssue> issues,
        MonsterDefinition monster)
    {
        if (monster.Weapons.Count == 0)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.monsters.no_weapon",
                $"Ruleset monster '{monster.Id}' has no weapon."));
        }

        AddDuplicateIdIssues(
            issues,
            monster.Weapons,
            "ruleset.monsters.duplicate_weapon",
            $"weapon on ruleset monster '{monster.Id}'",
            weapon => weapon.WeaponId);

        foreach (MonsterWeaponDefinition weapon in monster.Weapons)
        {
            AddRequiredStringIssue(
                issues,
                weapon.WeaponId,
                "ruleset.monsters.weapon_id_required",
                $"Ruleset monster '{monster.Id}' has a weapon without an ID.");

            // Ammunition is all-or-nothing: an item without a count, or a count
            // without an item, is an authoring slip rather than a valid weapon.
            bool hasItem = !string.IsNullOrWhiteSpace(weapon.AmmunitionItemId);
            bool hasQuantity = weapon.AmmunitionQuantity is not null;

            if (hasItem != hasQuantity)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.monsters.ammunition_incomplete",
                    $"Weapon '{weapon.WeaponId}' on ruleset monster '{monster.Id}' declares only half of its ammunition."));
            }

            if (weapon.AmmunitionQuantity < 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.monsters.ammunition_negative",
                    $"Weapon '{weapon.WeaponId}' on ruleset monster '{monster.Id}' has negative ammunition."));
            }
        }
    }
}
