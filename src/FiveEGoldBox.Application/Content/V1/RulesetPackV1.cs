namespace FiveEGoldBox.Application.Content.V1;

/// One ruleset content pack as it exists on disk.
///
/// Id and Name are null for an additive pack that only contributes more
/// content (spells, weapons, items, ...) to a base pack rather than
/// declaring a ruleset identity of its own. Exactly one pack among those
/// loaded together must supply both — see RulesetPackLoader.Merge.
internal sealed record RulesetPackV1
{
    public required int FormatVersion { get; init; }

    public string? Id { get; init; }

    public string? Name { get; init; }

    public IReadOnlyList<RaceDefinitionV1> Races { get; init; }
        = Array.Empty<RaceDefinitionV1>();

    public IReadOnlyList<ClassDefinitionV1> Classes { get; init; }
        = Array.Empty<ClassDefinitionV1>();

    public IReadOnlyList<BackgroundDefinitionV1> Backgrounds { get; init; }
        = Array.Empty<BackgroundDefinitionV1>();

    public IReadOnlyList<SkillDefinitionV1> Skills { get; init; }
        = Array.Empty<SkillDefinitionV1>();

    public IReadOnlyList<ArmorDefinitionV1> Armors { get; init; }
        = Array.Empty<ArmorDefinitionV1>();

    public IReadOnlyList<WeaponDefinitionV1> Weapons { get; init; }
        = Array.Empty<WeaponDefinitionV1>();

    public IReadOnlyList<SpellDefinitionV1> Spells { get; init; }
        = Array.Empty<SpellDefinitionV1>();

    public IReadOnlyList<EffectDefinitionV1> Effects { get; init; }
        = Array.Empty<EffectDefinitionV1>();

    public IReadOnlyList<FeatureDefinitionV1> Features { get; init; }
        = Array.Empty<FeatureDefinitionV1>();

    public IReadOnlyList<EquipmentItemDefinitionV1> EquipmentItems { get; init; }
        = Array.Empty<EquipmentItemDefinitionV1>();
}
