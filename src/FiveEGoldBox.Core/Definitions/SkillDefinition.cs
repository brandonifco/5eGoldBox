using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record SkillDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required Ability Ability { get; init; }
}
