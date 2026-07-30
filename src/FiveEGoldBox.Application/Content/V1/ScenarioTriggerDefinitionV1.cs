namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ScenarioTriggerDefinitionV1
{
    public required string TriggerId { get; init; }

    public required string DisplayName { get; init; }

    public required string LocationId { get; init; }

    public ExplorationFloorV1? Floor { get; init; }

    public GridPositionV1? Position { get; init; }

    public ExplorationFacingV1? RequiredFacing { get; init; }

    public required IReadOnlyList<string> RequiredProgressIds { get; init; }

    public required string ResultingProgressId { get; init; }

    public string? EncounterId { get; init; }
}
