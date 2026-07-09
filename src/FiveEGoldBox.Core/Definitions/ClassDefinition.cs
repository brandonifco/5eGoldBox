using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record ClassDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required DieType HitDie { get; init; }

    public IReadOnlyList<Ability> SavingThrowProficiencies { get; init; }
        = Array.Empty<Ability>();

    public IReadOnlyList<string> ArmorProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> WeaponProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> ToolProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> SkillChoices { get; init; }
        = Array.Empty<string>();

    public int NumberOfSkillChoices { get; init; }

    public IReadOnlyDictionary<int, IReadOnlyList<string>> FeaturesByLevel { get; init; }
        = new Dictionary<int, IReadOnlyList<string>>();
}