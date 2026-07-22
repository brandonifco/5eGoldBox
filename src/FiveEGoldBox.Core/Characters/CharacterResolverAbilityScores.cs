using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Internal;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static IReadOnlyDictionary<Ability, int> CalculateAbilityScores(
        CharacterDraft draft,
        RaceDefinition? selectedRace,
        SubraceDefinition? selectedSubrace)
    {
        Dictionary<Ability, int> abilityScores = draft.BaseAbilityScores.ToDictionary(
            pair => pair.Key,
            pair => pair.Value);

        AddAbilityScoreIncreases(
            abilityScores,
            selectedRace?.AbilityScoreIncreases);

        AddAbilityScoreIncreases(
            abilityScores,
            selectedSubrace?.AbilityScoreIncreases);

        return CoreCollectionProtection.ProtectDictionary(abilityScores);
    }

    private static void AddAbilityScoreIncreases(
        Dictionary<Ability, int> abilityScores,
        IReadOnlyList<AbilityScoreIncrease>? abilityScoreIncreases)
    {
        if (abilityScoreIncreases is null)
        {
            return;
        }

        foreach (AbilityScoreIncrease increase in abilityScoreIncreases)
        {
            abilityScores[increase.Ability] += increase.Amount;
        }
    }

    private static IReadOnlyDictionary<Ability, int> CalculateAbilityModifiers(
        IReadOnlyDictionary<Ability, int> abilityScores)
    {
        return CoreCollectionProtection.ProtectDictionary(
            abilityScores.Select(pair =>
                new KeyValuePair<Ability, int>(
                    pair.Key,
                    AbilityRules.GetModifier(pair.Value))));
    }
}
