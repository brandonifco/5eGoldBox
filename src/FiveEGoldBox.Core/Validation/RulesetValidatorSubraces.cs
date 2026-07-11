using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
    private static void AddSubraceIssues(
        List<ValidationIssue> issues,
        IReadOnlyList<RaceDefinition> races)
    {
        foreach (RaceDefinition race in races)
        {
            foreach (SubraceDefinition subrace in race.Subraces)
            {
                AddRequiredStringIssue(
                    issues,
                    subrace.Id,
                    "ruleset.races.subraces.id.required",
                    $"Ruleset race '{race.Id}' contains subrace with missing ID.");

                AddRequiredStringIssue(
                    issues,
                    subrace.Name,
                    "ruleset.races.subraces.name.required",
                    $"Ruleset race '{race.Id}' contains subrace with missing name.");
            }

            foreach (IGrouping<string, SubraceDefinition> duplicateGroup in race.Subraces
                .Where(subrace => !string.IsNullOrWhiteSpace(subrace.Id))
                .GroupBy(subrace => subrace.Id)
                .Where(group => group.Count() > 1))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "ruleset.races.subraces.duplicate_id",
                    $"Ruleset race '{race.Id}' contains duplicate subrace ID '{duplicateGroup.Key}'."));
            }
        }
    }
}
