namespace FiveEGoldBox.Core.Definitions;

public sealed record BackgroundDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// Player-facing prose for a character-creation screen, so a choice is
    /// more than a bare name to someone who does not already know 5e.
    /// Optional: content that predates this loads unchanged, and a validator
    /// reports what is missing rather than refusing to load it.
    public string? Description { get; init; }

    public IReadOnlyList<string> SkillProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> ToolProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Languages { get; init; }
        = Array.Empty<string>();

    public string? FeatureId { get; init; }
}
