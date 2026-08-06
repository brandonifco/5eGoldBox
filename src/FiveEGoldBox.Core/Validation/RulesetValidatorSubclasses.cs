using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddSubclassIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<ClassDefinition> classes)
    {
        foreach (ClassDefinition characterClass in classes)
        {
            foreach (SubclassDefinition subclass in characterClass.Subclasses)
            {
                AddRequiredStringIssue(
                    issues,
                    subclass.Id,
                    "ruleset.classes.subclasses.id.required",
                    $"Ruleset class '{characterClass.Id}' contains subclass with missing ID.");

                AddRequiredStringIssue(
                    issues,
                    subclass.Name,
                    "ruleset.classes.subclasses.name.required",
                    $"Ruleset class '{characterClass.Id}' contains subclass with missing name.");
            }

            foreach (IGrouping<string, SubclassDefinition> duplicateGroup in characterClass.Subclasses
                .Where(subclass => !string.IsNullOrWhiteSpace(subclass.Id))
                .GroupBy(subclass => subclass.Id)
                .Where(group => group.Count() > 1))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.classes.subclasses.duplicate_id",
                    $"Ruleset class '{characterClass.Id}' contains duplicate subclass ID '{duplicateGroup.Key}'."));
            }
        }
    }
}
