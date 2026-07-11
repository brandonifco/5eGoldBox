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

    public IReadOnlyList<SkillDefinition> Skills { get; init; }
        = Array.Empty<SkillDefinition>();

    public IReadOnlyList<ArmorDefinition> Armors { get; init; }
        = Array.Empty<ArmorDefinition>();

    public IReadOnlyList<WeaponDefinition> Weapons { get; init; }
        = Array.Empty<WeaponDefinition>();

    public IReadOnlyList<EquipmentItemDefinition> EquipmentItems { get; init; }
        = Array.Empty<EquipmentItemDefinition>();
}
