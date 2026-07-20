using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

public sealed record WatchtowerCombatOutcomeResult
{
    public required WatchtowerCombatOutcome Outcome { get; init; }

    public required ApplicationMode ResultingMode { get; init; }

    public required WatchtowerScenarioProgress ResultingProgress { get; init; }

    public required ApplicationSessionState State { get; init; }
}
