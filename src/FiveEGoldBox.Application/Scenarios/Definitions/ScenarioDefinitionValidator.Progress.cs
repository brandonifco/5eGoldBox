using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal static partial class ScenarioDefinitionValidator
{
    private static void AddProgressIssues(
        ScenarioDefinition definition,
        List<ValidationIssue> issues)
    {
        ScenarioProgressDefinition progress = definition.Progress;

        if (progress.ProgressIds.Count == 0)
        {
            issues.Add(Error(
                "scenario.progress.empty",
                "A scenario must declare at least one progress marker."));
            return;
        }

        foreach (string progressId in progress.ProgressIds)
        {
            AddIfBlank(
                issues,
                progressId,
                "scenario.progress.id_required",
                "Scenario progress IDs must not be blank.");
        }

        AddDuplicateIdIssues(
            issues,
            progress.ProgressIds,
            "scenario.progress.duplicate_id",
            "progress ID");

        HashSet<string> declared = ToSet(progress.ProgressIds);

        if (!declared.Contains(progress.InitialProgressId))
        {
            issues.Add(Error(
                "scenario.progress.initial_unknown",
                $"Initial progress '{progress.InitialProgressId}' is not a declared progress marker."));
        }

        AddConclusionIssues(definition, declared, issues);
    }

    private static void AddConclusionIssues(
        ScenarioDefinition definition,
        HashSet<string> declaredProgress,
        List<ValidationIssue> issues)
    {
        IReadOnlyList<ScenarioConclusionDefinition> conclusions =
            definition.Progress.Conclusions;

        if (conclusions.Count == 0)
        {
            issues.Add(Error(
                "scenario.progress.no_conclusion",
                "A scenario must declare at least one conclusion, or it can never end."));
        }

        AddDuplicateIdIssues(
            issues,
            conclusions.Select(conclusion => conclusion.ProgressId),
            "scenario.conclusion.duplicate_progress",
            "conclusion progress");

        HashSet<string> locations = ToSet(
            definition.Locations.Select(location => location.LocationId));

        foreach (ScenarioConclusionDefinition conclusion in conclusions)
        {
            if (!declaredProgress.Contains(conclusion.ProgressId))
            {
                issues.Add(Error(
                    "scenario.conclusion.progress_unknown",
                    $"Conclusion progress '{conclusion.ProgressId}' is not a declared progress marker."));
            }

            if (!locations.Contains(conclusion.LocationId))
            {
                issues.Add(Error(
                    "scenario.conclusion.location_unknown",
                    $"Conclusion location '{conclusion.LocationId}' is not a declared location."));
            }

            if (string.Equals(
                conclusion.ProgressId,
                definition.Progress.InitialProgressId,
                StringComparison.Ordinal))
            {
                issues.Add(Error(
                    "scenario.conclusion.initial_progress",
                    "A scenario cannot begin on a marker that already concludes it."));
            }
        }
    }

    private static HashSet<string> ToSet(
        IEnumerable<string> values)
    {
        return values.ToHashSet(StringComparer.Ordinal);
    }
}
