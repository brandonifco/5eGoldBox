namespace FiveEGoldBox.Application.Content.V1;

internal sealed record SkillDefinitionV1
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required AbilityV1 Ability { get; init; }

    public string? Description { get; init; }
}
