namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// The progress markers this scenario uses, and which one a new session starts
/// on. The engine treats markers as opaque strings; this is where a scenario
/// declares the vocabulary it will actually use.
internal sealed record ScenarioProgressDefinition
{
    internal required string InitialProgressId { get; init; }

    internal required IReadOnlyList<string> ProgressIds { get; init; }

    /// Markers that end the scenario, and whether each counts as success.
    internal required IReadOnlyList<ScenarioConclusionDefinition> Conclusions { get; init; }
}
