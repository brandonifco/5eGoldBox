namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ScenarioDecisionOptionDefinitionV1
{
    public required string OptionId { get; init; }

    public required string DisplayName { get; init; }

    public string? ResultingProgressId { get; init; }
}
