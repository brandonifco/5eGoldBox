using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddDefinitionReferenceIssues(
        List<ValidationIssue> issues,
        RulesetDefinition ruleset)
    {
        HashSet<string> skillIds = ruleset.Skills
            .Select(skill => skill.Id)
            .ToHashSet();

        AddUnknownReferenceIssues(
            issues,
            ruleset.Classes,
            characterClass => characterClass.SkillChoices,
            skillIds,
            "ruleset.classes.skill_choices.unknown_skill",
            "class",
            characterClass => characterClass.Id,
            "skill");

        AddUnknownReferenceIssues(
            issues,
            ruleset.Backgrounds,
            background => background.SkillProficiencies,
            skillIds,
            "ruleset.backgrounds.skill_proficiencies.unknown_skill",
            "background",
            background => background.Id,
            "skill");

        HashSet<string> armorProficiencyIds = ruleset.Armors
            .Select(armor => armor.Id)
            .Concat(
            [
                RuleIds.ArmorProficiencies.Light,
                RuleIds.ArmorProficiencies.Medium,
                RuleIds.ArmorProficiencies.Heavy,
                RuleIds.ArmorProficiencies.Shields
            ])
            .ToHashSet();

        AddUnknownReferenceIssues(
            issues,
            ruleset.Classes,
            characterClass => characterClass.ArmorProficiencies,
            armorProficiencyIds,
            "ruleset.classes.armor_proficiencies.unknown_armor",
            "class",
            characterClass => characterClass.Id,
            "armor proficiency");

        HashSet<string> weaponProficiencyIds = ruleset.Weapons
            .Select(weapon => weapon.Id)
            .Concat(
            [
                RuleIds.WeaponProficiencies.Simple,
                RuleIds.WeaponProficiencies.Martial
            ])
            .ToHashSet();

        AddUnknownReferenceIssues(
            issues,
            ruleset.Classes,
            characterClass => characterClass.WeaponProficiencies,
            weaponProficiencyIds,
            "ruleset.classes.weapon_proficiencies.unknown_weapon",
            "class",
            characterClass => characterClass.Id,
            "weapon proficiency");

        HashSet<string> equipmentItemIds = ruleset.EquipmentItems
            .Select(item => item.Id)
            .ToHashSet();

        AddUnknownOptionalReferenceIssues(
            issues,
            ruleset.Weapons,
            weapon => weapon.AmmunitionItemId,
            equipmentItemIds,
            "ruleset.weapons.ammunition_item.unknown_item",
            "weapon",
            weapon => weapon.Id,
            "equipment item");
    }
}
