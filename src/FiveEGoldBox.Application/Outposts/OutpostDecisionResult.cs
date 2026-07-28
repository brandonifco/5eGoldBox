using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Outposts;

public sealed record OutpostDecisionResult
{
    public required string OptionId { get; init; }

    public required bool DidProgressChange { get; init; }

    public required ApplicationSessionState State { get; init; }
}
