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

            // A route can only be entered in outpost mode, and nothing
            // currently transitions the party back into that mode once they
            // have left it — so a route whose origin is not the scenario's
            // own starting location can never actually be attempted. A real
            // return journey needs a new transition back into a hub-like
            // mode from exploration or travel; nothing here builds that yet.
            if (!string.Equals(
                route.OriginLocationId,
                definition.StartingLocationId,
                StringComparison.Ordinal))
            {
                issues.Add(Error(
                    "scenario.routes.origin_unreachable",
                    $"Route '{route.RouteId}' originates at '{route.OriginLocationId}', which the party can never be standing in outpost mode — only '{definition.StartingLocationId}' is. This route can never be attempted."));
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
