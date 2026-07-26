using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static int? CalculateMaxHitPoints(
        ClassDefinition? selectedClass,
        int level,
        IReadOnlyDictionary<Ability, int> abilityModifiers)
    {
        if (selectedClass is null)
        {
            return null;
        }

        int constitutionModifier = abilityModifiers[Ability.Constitution];
        int firstLevelHitPoints = Math.Max(
            1,
            (int)selectedClass.HitDie + constitutionModifier);

        if (level == 1)
        {
            return firstLevelHitPoints;
        }

        int additionalHitPointsPerLevel = Math.Max(
            1,
            HitDiceRules.GetFixedHitPointsAfterFirstLevel(
                selectedClass.HitDie)
                + constitutionModifier);

        return firstLevelHitPoints
            + ((level - 1) * additionalHitPointsPerLevel);
    }
}
