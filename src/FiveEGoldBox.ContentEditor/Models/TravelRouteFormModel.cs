using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Models;

/// A mutable, two-way-bindable stand-in for TravelRouteDefinitionV1 -- see
/// ScenarioLocationFormModel's header comment for why this is internal, not
/// public, unlike the ruleset form models.
internal sealed class TravelRouteFormModel
{
    public string RouteId { get; set; } = "";

    public string OriginLocationId { get; set; } = "";

    public string DestinationLocationId { get; set; } = "";

    public int FinalStepIndex { get; set; } = 1;

    public List<string> RequiredProgressIds { get; set; } = [];

    public static TravelRouteFormModel FromDefinition(
        TravelRouteDefinitionV1 route)
    {
        return new TravelRouteFormModel
        {
            RouteId = route.RouteId,
            OriginLocationId = route.OriginLocationId,
            DestinationLocationId = route.DestinationLocationId,
            FinalStepIndex = route.FinalStepIndex,
            RequiredProgressIds = [.. route.RequiredProgressIds]
        };
    }

    public TravelRouteDefinitionV1 ToDefinition()
    {
        return new TravelRouteDefinitionV1
        {
            RouteId = RouteId,
            OriginLocationId = OriginLocationId,
            DestinationLocationId = DestinationLocationId,
            FinalStepIndex = FinalStepIndex,
            RequiredProgressIds = RequiredProgressIds
        };
    }
}
