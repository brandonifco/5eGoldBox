namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// An overland journey between two locations, measured in steps.
internal sealed record TravelRouteDefinition
{
    internal required string RouteId { get; init; }

    internal required string OriginLocationId { get; init; }

    internal required string DestinationLocationId { get; init; }

    /// Index of the final step; a route of 3 takes four positions to walk.
    internal required int FinalStepIndex { get; init; }

    /// Progress the party must already have made before this route opens.
    internal IReadOnlyList<string> RequiredProgressIds { get; init; } =
        Array.Empty<string>();
}
