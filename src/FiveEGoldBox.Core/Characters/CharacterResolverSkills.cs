using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static IReadOnlyList<string> ResolveSkillProficiencies(
        CharacterDraft draft,
        ClassDefinition? selectedClass,
        BackgroundDefinition? selectedBackground)
    {
        return (selectedClass is null
                ? Array.Empty<string>()
                : draft.SelectedSkillIds)
            .Concat(selectedBackground?.SkillProficiencies ?? Array.Empty<string>())
            .Distinct()
            .ToArray();
    }

    private IReadOnlyList<SkillBonus> ResolveSkillBonuses(
        IReadOnlyList<string> skillProficiencies,
        IReadOnlyDictionary<Ability, int> abilityModifiers,
        int proficiencyBonus,
        ArmorDefinition? equippedArmor)
    {
        return _rulesetIndex is null
            ? Array.Empty<SkillBonus>()
            : _rulesetIndex.SkillsById.Values
                .Select(skill =>
                {
                    bool isProficient = skillProficiencies.Contains(skill.Id);
                    int abilityModifier = abilityModifiers[skill.Ability];

                    return new SkillBonus
                    {
                        SkillId = skill.Id,
                        SkillName = skill.Name,
                        Ability = skill.Ability,
                        AbilityModifier = abilityModifier,
                        IsProficient = isProficient,
                        ProficiencyBonus = isProficient ? proficiencyBonus : 0,
                        TotalBonus = abilityModifier + (isProficient ? proficiencyBonus : 0),
                        HasDisadvantage = skill.Id == RuleIds.Skills.Stealth
                            && (equippedArmor?.HasStealthDisadvantage ?? false)
                    };
                })
                .ToArray();
    }

    private static int CalculatePassivePerception(
        IReadOnlyList<SkillBonus> skillBonuses,
        IReadOnlyDictionary<Ability, int> abilityModifiers)
    {
        SkillBonus? perception = skillBonuses.SingleOrDefault(
            skill => skill.SkillId == RuleIds.Skills.Perception);

        if (perception is not null)
        {
            return 10 + perception.TotalBonus;
        }

        return 10 + abilityModifiers[Ability.Wisdom];
    }
}
