using System.Collections.ObjectModel;

namespace FiveEGoldBox.Core.Definitions;

internal static class RulesetDefinitionCanonicalizer
{
    public static RulesetDefinition Create(RulesetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return definition with
        {
            Races = Protect(definition.Races.Select(CopyRace)),
            Classes = Protect(definition.Classes.Select(CopyClass)),
            Backgrounds = Protect(definition.Backgrounds.Select(CopyBackground)),
            Skills = Protect(definition.Skills),
            Armors = Protect(definition.Armors),
            Weapons = Protect(definition.Weapons.Select(CopyWeapon)),
            EquipmentItems = Protect(definition.EquipmentItems.Select(CopyEquipmentItem))
        };
    }

    private static RaceDefinition CopyRace(RaceDefinition race)
    {
        return race with
        {
            AbilityScoreIncreases = Protect(race.AbilityScoreIncreases),
            Languages = Protect(race.Languages),
            Traits = Protect(race.Traits),
            Subraces = Protect(race.Subraces.Select(CopySubrace)),
            Senses = Protect(race.Senses),
            MovementSpeeds = Protect(race.MovementSpeeds),
            DamageResponses = Protect(race.DamageResponses),
            ConditionImmunities = Protect(race.ConditionImmunities)
        };
    }

    private static SubraceDefinition CopySubrace(SubraceDefinition subrace)
    {
        return subrace with
        {
            AbilityScoreIncreases = Protect(subrace.AbilityScoreIncreases),
            Languages = Protect(subrace.Languages),
            Traits = Protect(subrace.Traits),
            Senses = Protect(subrace.Senses),
            MovementSpeeds = Protect(subrace.MovementSpeeds),
            DamageResponses = Protect(subrace.DamageResponses),
            ConditionImmunities = Protect(subrace.ConditionImmunities)
        };
    }

    private static ClassDefinition CopyClass(ClassDefinition characterClass)
    {
        return characterClass with
        {
            SavingThrowProficiencies = Protect(characterClass.SavingThrowProficiencies),
            ArmorProficiencies = Protect(characterClass.ArmorProficiencies),
            WeaponProficiencies = Protect(characterClass.WeaponProficiencies),
            ToolProficiencies = Protect(characterClass.ToolProficiencies),
            SkillChoices = Protect(characterClass.SkillChoices),
            FeaturesByLevel = ProtectFeatures(characterClass.FeaturesByLevel)
        };
    }

    private static BackgroundDefinition CopyBackground(BackgroundDefinition background)
    {
        return background with
        {
            SkillProficiencies = Protect(background.SkillProficiencies),
            ToolProficiencies = Protect(background.ToolProficiencies),
            Languages = Protect(background.Languages)
        };
    }

    private static WeaponDefinition CopyWeapon(WeaponDefinition weapon)
    {
        return weapon with
        {
            Properties = Protect(weapon.Properties)
        };
    }

    private static EquipmentItemDefinition CopyEquipmentItem(
        EquipmentItemDefinition equipmentItem)
    {
        return equipmentItem with
        {
            Tags = Protect(equipmentItem.Tags)
        };
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<string>> ProtectFeatures(
        IReadOnlyDictionary<int, IReadOnlyList<string>> featuresByLevel)
    {
        Dictionary<int, IReadOnlyList<string>> protectedFeatures = featuresByLevel
            .ToDictionary(
                pair => pair.Key,
                pair => Protect(pair.Value));

        return new ReadOnlyDictionary<int, IReadOnlyList<string>>(protectedFeatures);
    }

    private static IReadOnlyList<T> Protect<T>(IEnumerable<T> values)
    {
        return Array.AsReadOnly(values.ToArray());
    }
}
