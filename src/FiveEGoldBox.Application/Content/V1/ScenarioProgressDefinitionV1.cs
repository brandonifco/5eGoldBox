namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ScenarioProgressDefinitionV1
{
    public required string InitialProgressId { get; init; }

    public required IReadOnlyList<string> ProgressIds { get; init; }

    public required IReadOnlyList<ScenarioConclusionDefinitionV1> Conclusions { get; init; }
}
