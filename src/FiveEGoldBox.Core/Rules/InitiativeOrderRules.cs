namespace FiveEGoldBox.Core.Rules;

public static class InitiativeOrderRules
{
    public static IReadOnlyList<InitiativeOrderEntry> ResolveOrder(
        IReadOnlyList<InitiativeOrderCombatant> combatants)
    {
        ArgumentNullException.ThrowIfNull(combatants);

        ValidateCombatants(combatants);

        Dictionary<int, int> initiativeTotalCounts = combatants
            .GroupBy(combatant => combatant.Initiative.Total)
            .ToDictionary(
                group => group.Key,
                group => group.Count());

        return combatants
            .Select((combatant, originalIndex) => new
            {
                Combatant = combatant,
                OriginalIndex = originalIndex
            })
            .OrderByDescending(entry => entry.Combatant.Initiative.Total)
            .ThenBy(entry => entry.OriginalIndex)
            .Select((entry, index) => new InitiativeOrderEntry
            {
                CombatantId = entry.Combatant.CombatantId,
                Initiative = entry.Combatant.Initiative,
                Position = index + 1,
                HasTiedInitiative = initiativeTotalCounts[entry.Combatant.Initiative.Total] > 1
            })
            .ToArray();
    }

    private static void ValidateCombatants(
        IReadOnlyList<InitiativeOrderCombatant> combatants)
    {
        HashSet<string> combatantIds = new(StringComparer.Ordinal);

        foreach (InitiativeOrderCombatant combatant in combatants)
        {
            ArgumentNullException.ThrowIfNull(combatant);

            if (string.IsNullOrWhiteSpace(combatant.CombatantId))
            {
                throw new ArgumentException(
                    "Combatant ID is required.",
                    nameof(combatants));
            }

            ArgumentNullException.ThrowIfNull(combatant.Initiative);

            if (!combatantIds.Add(combatant.CombatantId))
            {
                throw new ArgumentException(
                    $"Duplicate combatant ID '{combatant.CombatantId}' is not allowed.",
                    nameof(combatants));
            }
        }
    }
}
