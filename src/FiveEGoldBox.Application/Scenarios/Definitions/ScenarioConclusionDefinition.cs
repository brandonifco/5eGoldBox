namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal sealed record ScenarioConclusionDefinition
{
    internal required string ProgressId { get; init; }

    internal required bool IsSuccess { get; init; }

    /// Where the party stands when the scenario ends on this marker.
    internal required string LocationId { get; init; }
}
