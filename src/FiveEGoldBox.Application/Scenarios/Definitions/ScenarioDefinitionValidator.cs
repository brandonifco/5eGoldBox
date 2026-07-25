using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// Checks authored scenario content before anything tries to play it.
///
/// Every issue is collected rather than thrown on, because whoever is writing a
/// scenario wants the whole list, not the first mistake. Callers decide what to
/// do with a failed result; loading refuses, tooling reports.
internal static partial class ScenarioDefinitionValidator
{
    internal static ValidationResult Validate(
        ScenarioDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        List<ValidationIssue> issues = [];

        AddIdentityIssues(definition, issues);
        AddProgressIssues(definition, issues);
        AddPartyRequirementIssues(definition, issues);
        AddLocationIssues(definition, issues);
        AddRouteIssues(definition, issues);
        AddEncounterIssues(definition, issues);
        AddEncounterOutcomeIssues(definition, issues);
        AddTriggerIssues(definition, issues);
        AddDecisionIssues(definition, issues);
        AddProgressReachabilityIssues(definition, issues);

        return new ValidationResult(issues);
    }

    private static void AddIdentityIssues(
        ScenarioDefinition definition,
        List<ValidationIssue> issues)
    {
        AddIfBlank(
            issues,
            definition.ScenarioId,
            "scenario.id.required",
            "Scenario ID is required.");
        AddIfBlank(
            issues,
            definition.DisplayName,
            "scenario.display_name.required",
            "Scenario display name is required.");
        AddIfBlank(
            issues,
            definition.RulesetId,
            "scenario.ruleset_id.required",
            "Scenario ruleset ID is required.");

        if (!definition.Locations.Any(location => string.Equals(
            location.LocationId,
            definition.StartingLocationId,
            StringComparison.Ordinal)))
        {
            issues.Add(Error(
                "scenario.starting_location.unknown",
                $"Starting location '{definition.StartingLocationId}' is not a declared location."));
        }
    }

    private static void AddPartyRequirementIssues(
        ScenarioDefinition definition,
        List<ValidationIssue> issues)
    {
        PartyRequirementDefinition requirement = definition.PartyRequirement;

        if (requirement.MinimumMembers < 1)
        {
            issues.Add(Error(
                "scenario.party_requirement.minimum_members",
                "A scenario must require at least one party member."));
        }

        if (requirement.MaximumMembers < requirement.MinimumMembers)
        {
            issues.Add(Error(
                "scenario.party_requirement.maximum_members",
                "Maximum party members must not be below the minimum."));
        }

        if (requirement.MinimumConsciousMembers < 1
            || requirement.MinimumConsciousMembers > requirement.MinimumMembers)
        {
            issues.Add(Error(
                "scenario.party_requirement.minimum_conscious_members",
                "Minimum conscious members must be between one and the minimum party size."));
        }
    }

    private static void AddIfBlank(
        List<ValidationIssue> issues,
        string value,
        string code,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(code, message));
        }
    }

    private static void AddDuplicateIdIssues(
        List<ValidationIssue> issues,
        IEnumerable<string> ids,
        string code,
        string label)
    {
        foreach (IGrouping<string, string> duplicate in ids
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1))
        {
            issues.Add(Error(
                code,
                $"Scenario declares duplicate {label} '{duplicate.Key}'."));
        }
    }

    private static ValidationIssue Error(
        string code,
        string message)
    {
        return new ValidationIssue(
            ValidationSeverity.Error,
            code,
            message);
    }
}
