namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ClassDefinitionV1
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required DieTypeV1 HitDie { get; init; }

    public IReadOnlyList<AbilityV1> SavingThrowProficiencies { get; init; }
        = Array.Empty<AbilityV1>();

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

    public IReadOnlyList<SubclassDefinitionV1> Subclasses { get; init; }
        = Array.Empty<SubclassDefinitionV1>();

    public AbilityV1? SpellcastingAbility { get; init; }

    public IReadOnlyDictionary<int, int> SpellSlotsByLevel { get; init; }
        = new Dictionary<int, int>();
}
