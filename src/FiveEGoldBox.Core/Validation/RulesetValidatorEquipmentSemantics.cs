using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddEquipmentSemanticIssues(
        List<ValidationIssue> issues,
        RulesetDefinition ruleset)
    {
        foreach (ArmorDefinition armor in ruleset.Armors)
        {
            AddArmorSemanticIssues(issues, armor);
        }

        foreach (WeaponDefinition weapon in ruleset.Weapons)
        {
            AddWeaponRangeSemanticIssues(issues, weapon);
        }
    }

    private static void AddArmorSemanticIssues(
        List<ValidationIssue> issues,
        ArmorDefinition armor)
    {
        if (!armor.AddsDexterityModifier
            && armor.MaximumDexterityModifier.HasValue)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.armors.maximum_dexterity_modifier.unused",
                $"Ruleset armor '{armor.Id}' defines a maximum Dexterity modifier but does not add Dexterity modifier."));
        }

        if (armor.Category == ArmorCategory.Shield
            && armor.ArmorClassBonus <= 0)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.armors.shield_bonus.invalid",
                $"Ruleset shield '{armor.Id}' has invalid armor class bonus '{armor.ArmorClassBonus}'."));
        }
    }

    private static void AddWeaponRangeSemanticIssues(
        List<ValidationIssue> issues,
        WeaponDefinition weapon)
    {
        if (weapon.AttackKind == WeaponAttackKind.Ranged
            && !weapon.NormalRangeFeet.HasValue)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.weapons.range.required",
                $"Ruleset ranged weapon '{weapon.Id}' is missing normal range."));
        }

        if (weapon.LongRangeFeet.HasValue
            && !weapon.NormalRangeFeet.HasValue)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.weapons.long_range.requires_normal_range",
                $"Ruleset weapon '{weapon.Id}' defines long range without normal range."));
        }

        if (weapon.NormalRangeFeet.HasValue
            && weapon.LongRangeFeet.HasValue
            && weapon.LongRangeFeet.Value <= weapon.NormalRangeFeet.Value)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.weapons.long_range.invalid",
                $"Ruleset weapon '{weapon.Id}' has long range '{weapon.LongRangeFeet}' that is not greater than normal range '{weapon.NormalRangeFeet}'."));
        }
    }
}
