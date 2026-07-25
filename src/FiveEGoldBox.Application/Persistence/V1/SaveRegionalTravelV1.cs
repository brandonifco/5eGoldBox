namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveRegionalTravelV1
{
    public required string RouteId { get; init; }

    public required string OriginLocationId { get; init; }

    public required string DestinationLocationId { get; init; }

    public required int CurrentStepIndex { get; init; }

    public required int FinalStepIndex { get; init; }
}
