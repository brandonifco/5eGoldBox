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
        AddMonsterDefinitionIssues(issues, ruleset.Monsters);
        AddSpellDefinitionIssues(issues, ruleset.Spells, ruleset.Effects);
        AddEffectDefinitionIssues(issues, ruleset.Effects);
        AddFeatureDefinitionIssues(issues, ruleset.Features);
        AddEquipmentSemanticIssues(issues, ruleset);
        AddSubraceIssues(issues, ruleset.Races);
        AddSubclassIssues(issues, ruleset.Classes);
        AddDefinitionReferenceIssues(issues, ruleset);

        return issues.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(issues);
    }
}
