using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private RaceDefinition? GetSelectedRace(CharacterDraft draft)
    {
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.RaceId))
        {
            return null;
        }

        return _ruleset.Races.SingleOrDefault(race => race.Id == draft.RaceId);
    }

    private ClassDefinition? GetSelectedClass(CharacterDraft draft)
    {
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.ClassId))
        {
            return null;
        }

        return _ruleset.Classes.SingleOrDefault(characterClass => characterClass.Id == draft.ClassId);
    }

    private BackgroundDefinition? GetSelectedBackground(CharacterDraft draft)
    {
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.BackgroundId))
        {
            return null;
        }

        return _ruleset.Backgrounds.SingleOrDefault(background => background.Id == draft.BackgroundId);
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
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.EquippedArmorId))
        {
            return null;
        }

        return _ruleset.Armors.SingleOrDefault(armor => armor.Id == draft.EquippedArmorId);
    }

    private ArmorDefinition? GetEquippedShield(CharacterDraft draft)
    {
        if (_ruleset is null || string.IsNullOrWhiteSpace(draft.EquippedShieldId))
        {
            return null;
        }

        return _ruleset.Armors.SingleOrDefault(armor => armor.Id == draft.EquippedShieldId);
    }

    private IReadOnlyList<WeaponDefinition> GetEquippedWeapons(CharacterDraft draft)
    {
        if (_ruleset is null || draft.EquippedWeaponIds.Count == 0)
        {
            return Array.Empty<WeaponDefinition>();
        }

        return draft.EquippedWeaponIds
            .Select(weaponId => _ruleset.Weapons.SingleOrDefault(weapon => weapon.Id == weaponId))
            .Where(weapon => weapon is not null)
            .Cast<WeaponDefinition>()
            .ToArray();
    }
}