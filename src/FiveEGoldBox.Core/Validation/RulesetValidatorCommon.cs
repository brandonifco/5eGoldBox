namespace FiveEGoldBox.Core.Validation;

public static partial class RulesetValidator
{
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

    private static void AddUnknownOptionalReferenceIssues<TDefinition>(
        List<ValidationIssue> issues,
        IReadOnlyList<TDefinition> definitions,
        Func<TDefinition, string?> getReferencedId,
        IReadOnlySet<string> validIds,
        string issueCode,
        string definitionName,
        Func<TDefinition, string> getDefinitionId,
        string referencedDefinitionName)
    {
        foreach (TDefinition definition in definitions)
        {
            string? referencedId = getReferencedId(definition);

            if (string.IsNullOrWhiteSpace(referencedId)
                || validIds.Contains(referencedId))
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
