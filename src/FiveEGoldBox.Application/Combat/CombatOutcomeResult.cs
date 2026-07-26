using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatOutcomeResult
{
    public required CombatOutcome Outcome { get; init; }

    public required ApplicationMode ResultingMode { get; init; }

    /// Where the scenario now stands, as its own opaque progress identifier
    /// rather than any one scenario's enum.
    public required string ResultingProgressId { get; init; }

    public required ApplicationSessionState State { get; init; }
}
