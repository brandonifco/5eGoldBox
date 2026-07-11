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
            GetFixedHitPointsAfterFirstLevel(selectedClass.HitDie)
                + constitutionModifier);

        return firstLevelHitPoints
            + ((level - 1) * additionalHitPointsPerLevel);
    }

    private static int GetFixedHitPointsAfterFirstLevel(DieType hitDie)
    {
        return hitDie switch
        {
            DieType.D6 => 4,
            DieType.D8 => 5,
            DieType.D10 => 6,
            DieType.D12 => 7,
            _ => throw new InvalidOperationException($"Unsupported class hit die '{hitDie}'.")
        };
    }
}
