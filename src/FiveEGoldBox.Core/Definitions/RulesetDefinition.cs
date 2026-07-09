namespace FiveEGoldBox.Core.Definitions;

public sealed record RulesetDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<RaceDefinition> Races { get; init; }
        = Array.Empty<RaceDefinition>();

    public IReadOnlyList<ClassDefinition> Classes { get; init; }
        = Array.Empty<ClassDefinition>();

    public IReadOnlyList<BackgroundDefinition> Backgrounds { get; init; }
        = Array.Empty<BackgroundDefinition>();
}