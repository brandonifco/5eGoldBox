using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Travel;

public sealed record RegionalTravelAdvanceResult
{
    public required bool DidArrive { get; init; }

    public required ApplicationSessionState State { get; init; }
}
