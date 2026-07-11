namespace FiveEGoldBox.Core.Definitions;

public sealed class RulesetIndex
{
    public RulesetIndex(RulesetDefinition ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        Ruleset = ruleset;
        RacesById = ruleset.Races.ToDictionary(race => race.Id);
        ClassesById = ruleset.Classes.ToDictionary(characterClass => characterClass.Id);
        BackgroundsById = ruleset.Backgrounds.ToDictionary(background => background.Id);
        SkillsById = ruleset.Skills.ToDictionary(skill => skill.Id);
        ArmorsById = ruleset.Armors.ToDictionary(armor => armor.Id);
        WeaponsById = ruleset.Weapons.ToDictionary(weapon => weapon.Id);
        EquipmentItemsById = ruleset.EquipmentItems.ToDictionary(item => item.Id);
    }

    public RulesetDefinition Ruleset { get; }

    public IReadOnlyDictionary<string, RaceDefinition> RacesById { get; }

    public IReadOnlyDictionary<string, ClassDefinition> ClassesById { get; }

    public IReadOnlyDictionary<string, BackgroundDefinition> BackgroundsById { get; }

    public IReadOnlyDictionary<string, SkillDefinition> SkillsById { get; }

    public IReadOnlyDictionary<string, ArmorDefinition> ArmorsById { get; }

    public IReadOnlyDictionary<string, WeaponDefinition> WeaponsById { get; }

    public IReadOnlyDictionary<string, EquipmentItemDefinition> EquipmentItemsById { get; }
}