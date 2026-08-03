namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ScenarioConclusionDefinitionV1
{
    public required string ProgressId { get; init; }

    public required bool IsSuccess { get; init; }

    public required string LocationId { get; init; }

    public required string EpilogueText { get; init; }
}
