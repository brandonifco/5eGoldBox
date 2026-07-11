using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddRulesetIdentityIssues(
        List<ValidationIssue> issues,
        RulesetDefinition ruleset)
    {
        AddRequiredStringIssue(
            issues,
            ruleset.Id,
            "ruleset.id.required",
            "Ruleset ID is required.");

        AddRequiredStringIssue(
            issues,
            ruleset.Name,
            "ruleset.name.required",
            "Ruleset name is required.");
    }

    private static void AddRequiredDefinitionIdentityIssues(
        List<ValidationIssue> issues,
        RulesetDefinition ruleset)
    {
        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Races,
            "ruleset.races.id.required",
            "ruleset.races.name.required",
            "race",
            race => race.Id,
            race => race.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Classes,
            "ruleset.classes.id.required",
            "ruleset.classes.name.required",
            "class",
            characterClass => characterClass.Id,
            characterClass => characterClass.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Backgrounds,
            "ruleset.backgrounds.id.required",
            "ruleset.backgrounds.name.required",
            "background",
            background => background.Id,
            background => background.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Skills,
            "ruleset.skills.id.required",
            "ruleset.skills.name.required",
            "skill",
            skill => skill.Id,
            skill => skill.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Armors,
            "ruleset.armors.id.required",
            "ruleset.armors.name.required",
            "armor",
            armor => armor.Id,
            armor => armor.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.Weapons,
            "ruleset.weapons.id.required",
            "ruleset.weapons.name.required",
            "weapon",
            weapon => weapon.Id,
            weapon => weapon.Name);

        AddRequiredDefinitionStringIssues(
            issues,
            ruleset.EquipmentItems,
            "ruleset.equipment_items.id.required",
            "ruleset.equipment_items.name.required",
            "equipment item",
            equipmentItem => equipmentItem.Id,
            equipmentItem => equipmentItem.Name);
    }
}
