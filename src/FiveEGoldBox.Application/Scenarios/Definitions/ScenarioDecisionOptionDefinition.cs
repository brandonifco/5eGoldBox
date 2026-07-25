namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal sealed record ScenarioDecisionOptionDefinition
{
    internal required string OptionId { get; init; }

    internal required string DisplayName { get; init; }

    /// Progress after choosing this option. Null when the option declines and
    /// leaves the scenario where it stands.
    internal string? ResultingProgressId { get; init; }
}
