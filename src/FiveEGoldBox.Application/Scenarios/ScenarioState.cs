namespace FiveEGoldBox.Application.Scenarios;

/// Where the party has got to in the running scenario.
///
/// Progress is an opaque, scenario-defined marker. The engine only stores and
/// compares it; the meaning of any particular value belongs to the scenario
/// that authored it, which is what lets one engine run more than one scenario.
public sealed record ScenarioState
{
    public required string ProgressId { get; init; }
}
