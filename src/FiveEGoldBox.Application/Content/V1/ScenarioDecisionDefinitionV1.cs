namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ScenarioDecisionDefinitionV1
{
    public required string DecisionId { get; init; }

    public required string DisplayName { get; init; }

    public required string LocationId { get; init; }

    public required IReadOnlyList<string> RequiredProgressIds { get; init; }

    public required IReadOnlyList<ScenarioDecisionOptionDefinitionV1> Options { get; init; }
}
