using System.Collections.ObjectModel;

namespace FiveEGoldBox.Core.Definitions;

internal sealed class RulesetIndex
{
    public RulesetIndex(RulesetDefinition ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        Ruleset = RulesetDefinitionCanonicalizer.Create(ruleset);
        RacesById = CreateIndex(Ruleset.Races, race => race.Id);
        ClassesById = CreateIndex(Ruleset.Classes, characterClass => characterClass.Id);
        BackgroundsById = CreateIndex(Ruleset.Backgrounds, background => background.Id);
        SkillsById = CreateIndex(Ruleset.Skills, skill => skill.Id);
        ArmorsById = CreateIndex(Ruleset.Armors, armor => armor.Id);
        WeaponsById = CreateIndex(Ruleset.Weapons, weapon => weapon.Id);
        EquipmentItemsById = CreateIndex(Ruleset.EquipmentItems, item => item.Id);
    }

    public RulesetDefinition Ruleset { get; }

    public IReadOnlyDictionary<string, RaceDefinition> RacesById { get; }

    public IReadOnlyDictionary<string, ClassDefinition> ClassesById { get; }

    public IReadOnlyDictionary<string, BackgroundDefinition> BackgroundsById { get; }

    public IReadOnlyDictionary<string, SkillDefinition> SkillsById { get; }

    public IReadOnlyDictionary<string, ArmorDefinition> ArmorsById { get; }

    public IReadOnlyDictionary<string, WeaponDefinition> WeaponsById { get; }

    public IReadOnlyDictionary<string, EquipmentItemDefinition> EquipmentItemsById { get; }

    private static IReadOnlyDictionary<string, TDefinition> CreateIndex<TDefinition>(
        IEnumerable<TDefinition> definitions,
        Func<TDefinition, string> idSelector)
    {
        Dictionary<string, TDefinition> index = definitions.ToDictionary(idSelector);

        return new ReadOnlyDictionary<string, TDefinition>(index);
    }
}
