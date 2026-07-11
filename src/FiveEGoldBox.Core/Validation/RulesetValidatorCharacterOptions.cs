using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddCharacterOptionDefinitionIssues(
        List<ValidationIssue> issues,
        RulesetDefinition ruleset)
    {
        foreach (ClassDefinition characterClass in ruleset.Classes)
        {
            AddClassSkillChoiceCountIssues(issues, characterClass);
            AddClassFeatureIssues(issues, characterClass);
        }

        foreach (BackgroundDefinition background in ruleset.Backgrounds)
        {
            AddBackgroundFeatureIssue(issues, background);
        }
    }

    private static void AddClassSkillChoiceCountIssues(
        List<ValidationIssue> issues,
        ClassDefinition characterClass)
    {
        if (characterClass.NumberOfSkillChoices < 0)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.classes.skill_choice_count.invalid",
                $"Ruleset class '{characterClass.Id}' has invalid skill choice count '{characterClass.NumberOfSkillChoices}'."));
        }

        if (characterClass.NumberOfSkillChoices > characterClass.SkillChoices.Count)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.classes.skill_choice_count.exceeds_available",
                $"Ruleset class '{characterClass.Id}' requires {characterClass.NumberOfSkillChoices} skill choice(s), but only {characterClass.SkillChoices.Count} are available."));
        }
    }

    private static void AddClassFeatureIssues(
        List<ValidationIssue> issues,
        ClassDefinition characterClass)
    {
        foreach (KeyValuePair<int, IReadOnlyList<string>> featuresForLevel in characterClass.FeaturesByLevel)
        {
            if (featuresForLevel.Key <= 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.classes.features.level.invalid",
                    $"Ruleset class '{characterClass.Id}' has invalid feature level '{featuresForLevel.Key}'."));
            }

            foreach (string featureId in featuresForLevel.Value)
            {
                AddRequiredStringIssue(
                    issues,
                    featureId,
                    "ruleset.classes.features.feature_id.required",
                    $"Ruleset class '{characterClass.Id}' contains a blank feature ID at level '{featuresForLevel.Key}'.");
            }
        }
    }

    private static void AddBackgroundFeatureIssue(
        List<ValidationIssue> issues,
        BackgroundDefinition background)
    {
        if (background.FeatureId is null)
        {
            return;
        }

        AddRequiredStringIssue(
            issues,
            background.FeatureId,
            "ruleset.backgrounds.feature_id.required",
            $"Ruleset background '{background.Id}' has blank feature ID.");
    }
}
