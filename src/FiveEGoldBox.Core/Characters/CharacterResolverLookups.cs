using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private RaceDefinition? GetSelectedRace(CharacterDraft draft)
    {
        if (_rulesetIndex is null || string.IsNullOrWhiteSpace(draft.RaceId))
        {
            return null;
        }

        return _rulesetIndex.RacesById.GetValueOrDefault(draft.RaceId);
    }

    private ClassDefinition? GetSelectedClass(CharacterDraft draft)
    {
        if (_rulesetIndex is null || string.IsNullOrWhiteSpace(draft.ClassId))
        {
            return null;
        }

        return _rulesetIndex.ClassesById.GetValueOrDefault(draft.ClassId);
    }

    private BackgroundDefinition? GetSelectedBackground(CharacterDraft draft)
    {
        if (_rulesetIndex is null || string.IsNullOrWhiteSpace(draft.BackgroundId))
        {
            return null;
        }

        return _rulesetIndex.BackgroundsById.GetValueOrDefault(draft.BackgroundId);
    }

    private static SubraceDefinition? GetSelectedSubrace(
        CharacterDraft draft,
        RaceDefinition? selectedRace)
    {
        if (selectedRace is null || string.IsNullOrWhiteSpace(draft.SubraceId))
        {
            return null;
        }

        return selectedRace.Subraces.SingleOrDefault(subrace => subrace.Id == draft.SubraceId);
    }

    private ArmorDefinition? GetEquippedArmor(CharacterDraft draft)
    {
        if (_rulesetIndex is null || string.IsNullOrWhiteSpace(draft.EquippedArmorId))
        {
            return null;
        }

        return _rulesetIndex.ArmorsById.GetValueOrDefault(draft.EquippedArmorId);
    }

    private ArmorDefinition? GetEquippedShield(CharacterDraft draft)
    {
        if (_rulesetIndex is null || string.IsNullOrWhiteSpace(draft.EquippedShieldId))
        {
            return null;
        }

        return _rulesetIndex.ArmorsById.GetValueOrDefault(draft.EquippedShieldId);
    }

    private IReadOnlyList<WeaponDefinition> GetEquippedWeapons(CharacterDraft draft)
    {
        if (_rulesetIndex is null || draft.EquippedWeaponIds.Count == 0)
        {
            return Array.Empty<WeaponDefinition>();
        }

        return draft.EquippedWeaponIds
            .Select(weaponId => _rulesetIndex.WeaponsById.GetValueOrDefault(weaponId))
            .Where(weapon => weapon is not null)
            .Cast<WeaponDefinition>()
            .ToArray();
    }
}