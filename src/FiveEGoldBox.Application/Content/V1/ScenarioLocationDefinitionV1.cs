namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ScenarioLocationDefinitionV1
{
    public required string LocationId { get; init; }

    public required string DisplayName { get; init; }

    public ExplorationMapDefinitionV1? ExplorationMap { get; init; }

    public IReadOnlyList<string> ExplorableProgressIds { get; init; }
        = Array.Empty<string>();
}
