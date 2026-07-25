using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal static partial class ScenarioDefinitionValidator
{
    private static void AddDecisionIssues(
        ScenarioDefinition definition,
        List<ValidationIssue> issues)
    {
        AddDuplicateIdIssues(
            issues,
            definition.Decisions.Select(decision => decision.DecisionId),
            "scenario.decisions.duplicate_id",
            "decision ID");

        HashSet<string> locations = ToSet(
            definition.Locations.Select(location => location.LocationId));
        HashSet<string> progress = ToSet(definition.Progress.ProgressIds);

        foreach (ScenarioDecisionDefinition decision in definition.Decisions)
        {
            string subject = $"Decision '{decision.DecisionId}'";

            AddIfBlank(
                issues,
                decision.DecisionId,
                "scenario.decisions.id_required",
                "Decision IDs must not be blank.");

            if (!locations.Contains(decision.LocationId))
            {
                issues.Add(Error(
                    "scenario.decisions.location_unknown",
                    $"{subject} is offered at undeclared location '{decision.LocationId}'."));
            }

            AddUnknownProgressIssues(
                issues,
                decision.RequiredProgressIds,
                progress,
                "scenario.decisions.required_progress_unknown",
                subject);

            if (decision.Options.Count == 0)
            {
                issues.Add(Error(
                    "scenario.decisions.no_options",
                    $"{subject} offers nothing to choose."));
            }

            AddDuplicateIdIssues(
                issues,
                decision.Options.Select(option => option.OptionId),
                "scenario.decisions.duplicate_option",
                $"option on {subject}");

            foreach (ScenarioDecisionOptionDefinition option in decision.Options
                .Where(option => option.ResultingProgressId is not null
                    && !progress.Contains(option.ResultingProgressId)))
            {
                issues.Add(Error(
                    "scenario.decisions.option_progress_unknown",
                    $"Option '{option.OptionId}' on {subject} advances to undeclared progress '{option.ResultingProgressId}'."));
            }
        }
    }

    private static void AddEncounterOutcomeIssues(
        ScenarioDefinition definition,
        List<ValidationIssue> issues)
    {
        HashSet<string> progress = ToSet(definition.Progress.ProgressIds);

        foreach (EncounterDefinition encounter in definition.Encounters)
        {
            foreach (string progressId in new[]
            {
                encounter.Outcome.VictoryProgressId,
                encounter.Outcome.DefeatProgressId
            }.Where(progressId => !progress.Contains(progressId)))
            {
                issues.Add(Error(
                    "scenario.encounters.outcome_progress_unknown",
                    $"Encounter '{encounter.EncounterId}' resolves to undeclared progress '{progressId}'."));
            }

            if (string.Equals(
                encounter.Outcome.VictoryProgressId,
                encounter.Outcome.DefeatProgressId,
                StringComparison.Ordinal))
            {
                issues.Add(Error(
                    "scenario.encounters.outcome_indistinguishable",
                    $"Encounter '{encounter.EncounterId}' leaves the scenario in the same place whether it is won or lost."));
            }
        }
    }

    /// A marker nothing can produce is content that will never be reached.
    /// Reported as a warning rather than an error: it is almost always an
    /// oversight, but a scenario with a marker reserved for later still loads
    /// and plays correctly.
    private static void AddProgressReachabilityIssues(
        ScenarioDefinition definition,
        List<ValidationIssue> issues)
    {
        HashSet<string> producible = [definition.Progress.InitialProgressId];

        foreach (ScenarioTriggerDefinition trigger in definition.Triggers)
        {
            producible.Add(trigger.ResultingProgressId);
        }

        foreach (ScenarioDecisionOptionDefinition option in definition.Decisions
            .SelectMany(decision => decision.Options)
            .Where(option => option.ResultingProgressId is not null))
        {
            producible.Add(option.ResultingProgressId!);
        }

        foreach (EncounterDefinition encounter in definition.Encounters)
        {
            producible.Add(encounter.Outcome.VictoryProgressId);
            producible.Add(encounter.Outcome.DefeatProgressId);
        }

        foreach (string progressId in definition.Progress.ProgressIds
            .Where(progressId => !producible.Contains(progressId)))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "scenario.progress.unreachable",
                $"Progress '{progressId}' is declared but nothing in the scenario produces it."));
        }
    }
}
