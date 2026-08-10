using FiveEGoldBox.Core.Characters;

namespace FiveEGoldBox.Application.Combat;

/// Shared between EncounterCombatDecisionFactory and CombatViewFactory,
/// which independently build the same spell options for the write and the
/// read-only Query() paths — one algorithm rather than two copies that
/// could drift.
internal static class SpellTargetCombinationRules
{
    /// Every legal set of two or more targets a multi-target spell could
    /// reach, for a player to choose among directly — a caster who wants to
    /// Bless two allies rather than three should be able to say so, rather
    /// than the engine choosing for them.
    ///
    /// Gated on AppliedEffectId rather than just MaximumTargets: a damage
    /// spell can also name additional targets, but nothing in Core actually
    /// splits its damage across them yet — ResolveDamage and ResolveHealing
    /// both land their whole total on the single primary target regardless
    /// of how many are named. Offering combinations for a spell like that
    /// would let a player choose targets that silently do nothing, so this
    /// only builds them for spells whose effect actually reaches every
    /// target named — ApplyEffect already does, for every target in
    /// allTargets.
    internal static IReadOnlyList<IReadOnlyList<string>> Create(
        SpellAttack spell,
        IReadOnlyList<string> legalTargetIds)
    {
        if (spell.MaximumTargets <= 1
            || spell.AppliedEffectId is null)
        {
            return Array.Empty<IReadOnlyList<string>>();
        }

        int maximumSize = Math.Min(
            spell.MaximumTargets,
            legalTargetIds.Count);

        List<IReadOnlyList<string>> combinations = [];

        for (int size = 2; size <= maximumSize; size++)
        {
            foreach (string[] combination
                in EnumerateCombinations(legalTargetIds, size))
            {
                combinations.Add(Array.AsReadOnly(combination));
            }
        }

        return Array.AsReadOnly(combinations.ToArray());
    }

    /// Every combination of the given size, in lexicographic index order.
    /// Candidate counts here are party-sized — small enough that generating
    /// every subset outright is simpler than anything cleverer would be.
    private static IEnumerable<string[]> EnumerateCombinations(
        IReadOnlyList<string> items,
        int size)
    {
        int[] indices = Enumerable.Range(0, size).ToArray();

        while (true)
        {
            yield return indices
                .Select(index => items[index])
                .ToArray();

            int cursor = size - 1;

            while (cursor >= 0
                && indices[cursor] == items.Count - size + cursor)
            {
                cursor--;
            }

            if (cursor < 0)
            {
                yield break;
            }

            indices[cursor]++;

            for (int next = cursor + 1; next < size; next++)
            {
                indices[next] = indices[next - 1] + 1;
            }
        }
    }
}
