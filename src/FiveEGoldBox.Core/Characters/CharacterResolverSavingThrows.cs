using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static IReadOnlyList<SavingThrowBonus> ResolveSavingThrowBonuses(
        ClassDefinition? selectedClass,
        IReadOnlyDictionary<Ability, int> abilityModifiers,
        int proficiencyBonus)
    {
        return Enum
            .GetValues<Ability>()
            .Select(ability =>
            {
                bool isProficient = selectedClass?.SavingThrowProficiencies.Contains(ability) ?? false;
                int abilityModifier = abilityModifiers[ability];

                return new SavingThrowBonus
                {
                    Ability = ability,
                    AbilityModifier = abilityModifier,
                    IsProficient = isProficient,
                    ProficiencyBonus = isProficient ? proficiencyBonus : 0,
                    TotalBonus = abilityModifier + (isProficient ? proficiencyBonus : 0)
                };
            })
            .ToArray();
    }
}
