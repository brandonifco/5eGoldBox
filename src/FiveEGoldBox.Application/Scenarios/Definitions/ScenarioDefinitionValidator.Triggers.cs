using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal static partial class ScenarioDefinitionValidator
{
    private static void AddTriggerIssues(
        ScenarioDefinition definition,
        List<ValidationIssue> issues)
    {
        AddDuplicateIdIssues(
            issues,
            definition.Triggers.Select(trigger => trigger.TriggerId),
            "scenario.triggers.duplicate_id",
            "trigger ID");

        HashSet<string> progress = ToSet(definition.Progress.ProgressIds);
        HashSet<string> encounters = ToSet(
            definition.Encounters.Select(encounter => encounter.EncounterId));

        foreach (ScenarioTriggerDefinition trigger in definition.Triggers)
        {
            string subject = $"Trigger '{trigger.TriggerId}'";

            AddIfBlank(
                issues,
                trigger.TriggerId,
                "scenario.triggers.id_required",
                "Trigger IDs must not be blank.");

            AddLocationReferenceIssues(definition, trigger, subject, issues);

            AddUnknownProgressIssues(
                issues,
                trigger.RequiredProgressIds,
                progress,
                "scenario.triggers.required_progress_unknown",
                subject);

            if (!progress.Contains(trigger.ResultingProgressId))
            {
                issues.Add(Error(
                    "scenario.triggers.resulting_progress_unknown",
                    $"{subject} advances to undeclared progress '{trigger.ResultingProgressId}'."));
            }

            if (trigger.RequiredProgressIds.Contains(
                trigger.ResultingProgressId,
                StringComparer.Ordinal))
            {
                issues.Add(Error(
                    "scenario.triggers.no_advance",
                    $"{subject} advances to progress it already requires, so it can never move the scenario on."));
            }

            if (trigger.EncounterId is not null
                && !encounters.Contains(trigger.EncounterId))
            {
                issues.Add(Error(
                    "scenario.triggers.encounter_unknown",
                    $"{subject} starts undeclared encounter '{trigger.EncounterId}'."));
            }
        }

        AddUnreachableEncounterIssues(definition, issues);
    }

    /// A trigger fixed to a square must sit in a location that has a map, on a
    /// floor that map declares, somewhere the party can actually stand.
    private static void AddLocationReferenceIssues(
        ScenarioDefinition definition,
        ScenarioTriggerDefinition trigger,
        string subject,
        List<ValidationIssue> issues)
    {
        ScenarioLocationDefinition? location = definition.Locations
            .FirstOrDefault(candidate => string.Equals(
                candidate.LocationId,
                trigger.LocationId,
                StringComparison.Ordinal));

        if (location is null)
        {
            issues.Add(Error(
                "scenario.triggers.location_unknown",
                $"{subject} sits in undeclared location '{trigger.LocationId}'."));
            return;
        }

        if (trigger.Position is null && trigger.Floor is null)
        {
            return;
        }

        if (location.ExplorationMap is null)
        {
            issues.Add(Error(
                "scenario.triggers.location_not_explorable",
                $"{subject} is fixed to a square, but location '{trigger.LocationId}' has no map."));
            return;
        }

        ExplorationFloorDefinition? floor = trigger.Floor is null
            ? null
            : location.ExplorationMap.Floors.FirstOrDefault(
                candidate => candidate.Floor == trigger.Floor);

        if (trigger.Floor is not null && floor is null)
        {
            issues.Add(Error(
                "scenario.triggers.floor_unknown",
                $"{subject} sits on floor {trigger.Floor}, which its map does not declare."));
            return;
        }

        if (trigger.Position is not null
            && floor is not null
            && !floor.TraversablePositions.Contains(trigger.Position.Value))
        {
            issues.Add(Error(
                "scenario.triggers.position_impassable",
                $"{subject} sits on a square the party cannot reach."));
        }
    }

    /// An encounter nothing can start is authored content that will never be
    /// played, which is almost always a mistake rather than an intention.
    private static void AddUnreachableEncounterIssues(
        ScenarioDefinition definition,
        List<ValidationIssue> issues)
    {
        HashSet<string> started = ToSet(definition.Triggers
            .Where(trigger => trigger.EncounterId is not null)
            .Select(trigger => trigger.EncounterId!));

        foreach (EncounterDefinition encounter in definition.Encounters
            .Where(encounter => !started.Contains(encounter.EncounterId)))
        {
            issues.Add(Error(
                "scenario.encounters.unreachable",
                $"Encounter '{encounter.EncounterId}' is never started by any trigger."));
        }
    }
}
