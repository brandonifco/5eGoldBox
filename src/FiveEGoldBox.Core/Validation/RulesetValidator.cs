using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Validation;

public static class RulesetValidator
{
    public static ValidationResult Validate(RulesetDefinition ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        List<ValidationIssue> issues = [];

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

        return issues.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(issues);
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
}