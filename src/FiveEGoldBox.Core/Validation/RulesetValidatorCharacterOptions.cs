using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddCharacterOptionDefinitionIssues(
        List<ValidationIssue> issues,
        RulesetDefinition ruleset)
    {
        HashSet<string> declaredFeatures = new(
            ruleset.Features.Select(feature => feature.Id),
            StringComparer.Ordinal);

        foreach (ClassDefinition characterClass in ruleset.Classes)
        {
            AddClassHitDieIssue(issues, characterClass);
            AddClassSkillChoiceCountIssues(issues, characterClass);
            AddClassFeatureIssues(
                issues,
                characterClass,
                declaredFeatures);
        }

        foreach (BackgroundDefinition background in ruleset.Backgrounds)
        {
            AddBackgroundFeatureIssue(
                issues,
                background,
                declaredFeatures);
        }
    }

    /// A class whose hit die nothing can size would otherwise get as far as
    /// somebody rolling up a character before it failed.
    private static void AddClassHitDieIssue(
        List<ValidationIssue> issues,
        ClassDefinition characterClass)
    {
        if (HitDiceRules.IsSupported(characterClass.HitDie))
        {
            return;
        }

        issues.Add(new ValidationIssue(
            ValidationSeverity.Error,
            "ruleset.classes.hit_die.unsupported",
            $"Ruleset class '{characterClass.Id}' has unsupported hit die '{characterClass.HitDie}'."));
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

    /// A feature ID that names nothing was inert for as long as nothing read
    /// these. Now that a class feature can change a roll, an ID pointing
    /// nowhere is content that silently does less than it says.
    private static void AddClassFeatureIssues(
        List<ValidationIssue> issues,
        ClassDefinition characterClass,
        HashSet<string> declaredFeatures)
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

                if (!string.IsNullOrWhiteSpace(featureId)
                    && !declaredFeatures.Contains(featureId))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "ruleset.classes.features.feature_unknown",
                        $"Ruleset class '{characterClass.Id}' has undeclared feature '{featureId}' at level '{featuresForLevel.Key}'."));
                }
            }
        }
    }

    private static void AddBackgroundFeatureIssue(
        List<ValidationIssue> issues,
        BackgroundDefinition background,
        HashSet<string> declaredFeatures)
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

        // One rule for both, rather than class features resolving and
        // background features staying inert — the difference would be
        // arbitrary and somebody would trip on it.
        if (!string.IsNullOrWhiteSpace(background.FeatureId)
            && !declaredFeatures.Contains(background.FeatureId))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "ruleset.backgrounds.feature_unknown",
                $"Ruleset background '{background.Id}' has undeclared feature '{background.FeatureId}'."));
        }
    }
}
