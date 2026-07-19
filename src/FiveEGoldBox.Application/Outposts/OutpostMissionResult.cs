using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Outposts;

public sealed record OutpostMissionResult
{
    public required OutpostMissionChoice Choice { get; init; }

    public required bool DidProgressChange { get; init; }

    public required ApplicationSessionState State { get; init; }
}
