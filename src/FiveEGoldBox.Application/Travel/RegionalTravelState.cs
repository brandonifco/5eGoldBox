namespace FiveEGoldBox.Application.Travel;

public sealed record RegionalTravelState
{
    public required string RouteId { get; init; }

    public required string OriginLocationId { get; init; }

    public required string DestinationLocationId { get; init; }

    public required int CurrentStepIndex { get; init; }

    public required int FinalStepIndex { get; init; }

    public bool IsComplete =>
        CurrentStepIndex == FinalStepIndex;
}
