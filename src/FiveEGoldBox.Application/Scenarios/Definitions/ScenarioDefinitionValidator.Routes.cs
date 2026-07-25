using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal static partial class ScenarioDefinitionValidator
{
    private static void AddRouteIssues(
        ScenarioDefinition definition,
        List<ValidationIssue> issues)
    {
        AddDuplicateIdIssues(
            issues,
            definition.Routes.Select(route => route.RouteId),
            "scenario.routes.duplicate_id",
            "route ID");

        HashSet<string> locations = ToSet(
            definition.Locations.Select(location => location.LocationId));
        HashSet<string> progress = ToSet(definition.Progress.ProgressIds);

        foreach (TravelRouteDefinition route in definition.Routes)
        {
            AddIfBlank(
                issues,
                route.RouteId,
                "scenario.routes.id_required",
                "Route IDs must not be blank.");

            if (!locations.Contains(route.OriginLocationId))
            {
                issues.Add(Error(
                    "scenario.routes.origin_unknown",
                    $"Route '{route.RouteId}' starts at undeclared location '{route.OriginLocationId}'."));
            }

            if (!locations.Contains(route.DestinationLocationId))
            {
                issues.Add(Error(
                    "scenario.routes.destination_unknown",
                    $"Route '{route.RouteId}' ends at undeclared location '{route.DestinationLocationId}'."));
            }

            if (string.Equals(
                route.OriginLocationId,
                route.DestinationLocationId,
                StringComparison.Ordinal))
            {
                issues.Add(Error(
                    "scenario.routes.circular",
                    $"Route '{route.RouteId}' starts and ends at the same location."));
            }

            if (route.FinalStepIndex < 1)
            {
                issues.Add(Error(
                    "scenario.routes.final_step",
                    $"Route '{route.RouteId}' must take at least one step to walk."));
            }

            AddUnknownProgressIssues(
                issues,
                route.RequiredProgressIds,
                progress,
                "scenario.routes.required_progress_unknown",
                $"Route '{route.RouteId}'");
        }
    }

    private static void AddUnknownProgressIssues(
        List<ValidationIssue> issues,
        IEnumerable<string> referenced,
        HashSet<string> declared,
        string code,
        string subject)
    {
        foreach (string progressId in referenced
            .Where(progressId => !declared.Contains(progressId)))
        {
            issues.Add(Error(
                code,
                $"{subject} refers to undeclared progress '{progressId}'."));
        }
    }
}
