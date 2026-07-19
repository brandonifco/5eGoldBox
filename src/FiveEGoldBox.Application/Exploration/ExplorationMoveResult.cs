using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Exploration;

public sealed record ExplorationMoveResult
{
    public required bool DidMove { get; init; }

    public required ApplicationSessionState State { get; init; }
}
