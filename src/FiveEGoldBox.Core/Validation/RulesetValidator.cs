using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
namespace FiveEGoldBox.Core.Validation;

public static class RulesetValidator
{
    public static ValidationResult Validate(RulesetDefinition ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        List<ValidationIssue> issues = [];
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

        AddDuplicateIdIssues(
            issues,
            ruleset.Races,
            "ruleset.races.duplicate_id",
            "race",
            race => race.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Classes,
            "ruleset.classes.duplicate_id",
            "class",
            characterClass => characterClass.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Backgrounds,
            "ruleset.backgrounds.duplicate_id",
            "background",
            background => background.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Skills,
            "ruleset.skills.duplicate_id",
            "skill",
            skill => skill.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Armors,
            "ruleset.armors.duplicate_id",
            "armor",
            armor => armor.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.Weapons,
            "ruleset.weapons.duplicate_id",
            "weapon",
            weapon => weapon.Id);

        AddDuplicateIdIssues(
            issues,
            ruleset.EquipmentItems,
            "ruleset.equipment_items.duplicate_id",
            "equipment item",
            equipmentItem => equipmentItem.Id);
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
            
        return issues.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(issues);
    }
        private static void AddRequiredStringIssue(
        List<ValidationIssue> issues,
        string? value,
        string issueCode,
        string message)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        issues.Add(new ValidationIssue(
            ValidationSeverity.Error,
            issueCode,
            message));
    }

    private static void AddRequiredDefinitionStringIssues<TDefinition>(
        List<ValidationIssue> issues,
        IReadOnlyList<TDefinition> definitions,
        string missingIdIssueCode,
        string missingNameIssueCode,
        string definitionName,
        Func<TDefinition, string> getId,
        Func<TDefinition, string> getName)
    {
        foreach (TDefinition definition in definitions)
        {
            AddRequiredStringIssue(
                issues,
                getId(definition),
                missingIdIssueCode,
                $"Ruleset contains {definitionName} with missing ID.");

            AddRequiredStringIssue(
                issues,
                getName(definition),
                missingNameIssueCode,
                $"Ruleset contains {definitionName} with missing name.");
        }
    }
    private static void AddDuplicateIdIssues<TDefinition>(
        List<ValidationIssue> issues,
        IReadOnlyList<TDefinition> definitions,
        string issueCode,
        string definitionName,
        Func<TDefinition, string> getId)
    {
        foreach (IGrouping<string, TDefinition> duplicateGroup in definitions
            .GroupBy(getId)
            .Where(group => group.Count() > 1))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                issueCode,
                $"Ruleset contains duplicate {definitionName} ID '{duplicateGroup.Key}'."));
        }
    }
        private static void AddUnknownReferenceIssues<TDefinition>(
        List<ValidationIssue> issues,
        IReadOnlyList<TDefinition> definitions,
        Func<TDefinition, IReadOnlyList<string>> getReferencedIds,
        IReadOnlySet<string> validIds,
        string issueCode,
        string definitionName,
        Func<TDefinition, string> getDefinitionId,
        string referencedDefinitionName)
    {
        foreach (TDefinition definition in definitions)
        {
            foreach (string referencedId in getReferencedIds(definition))
            {
                if (validIds.Contains(referencedId))
                {
                    continue;
                }

                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    issueCode,
                    $"Ruleset {definitionName} '{getDefinitionId(definition)}' references unknown {referencedDefinitionName} ID '{referencedId}'."));
            }
        }
    }
}