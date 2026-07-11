namespace FiveEGoldBox.Core.Definitions;

public sealed record BackgroundDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<string> SkillProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> ToolProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Languages { get; init; }
        = Array.Empty<string>();

    public string? FeatureId { get; init; }
}
