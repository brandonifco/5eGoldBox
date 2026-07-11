using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    public static ValidationResult Validate(RulesetDefinition ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        List<ValidationIssue> issues = [];

        AddRulesetIdentityIssues(issues, ruleset);
        AddRequiredDefinitionIdentityIssues(issues, ruleset);
        AddDuplicateDefinitionIdIssues(issues, ruleset);
        AddCharacterOptionDefinitionIssues(issues, ruleset);
        AddNumericDefinitionIssues(issues, ruleset);
        AddWeaponDefinitionIssues(issues, ruleset.Weapons);
        AddEquipmentSemanticIssues(issues, ruleset);
        AddSubraceIssues(issues, ruleset.Races);
        AddDefinitionReferenceIssues(issues, ruleset);

        return issues.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(issues);
    }
}
