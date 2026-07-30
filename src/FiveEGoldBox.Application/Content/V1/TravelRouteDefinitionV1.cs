namespace FiveEGoldBox.Application.Content.V1;

internal sealed record TravelRouteDefinitionV1
{
    public required string RouteId { get; init; }

    public required string OriginLocationId { get; init; }

    public required string DestinationLocationId { get; init; }

    public required int FinalStepIndex { get; init; }

    public IReadOnlyList<string> RequiredProgressIds { get; init; }
        = Array.Empty<string>();
}
