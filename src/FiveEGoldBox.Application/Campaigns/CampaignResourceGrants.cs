using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Campaigns;

/// What a character's build grants it to spend.
///
/// One place answers this, so the factory that fills a character's resources
/// and the validator that checks them cannot disagree about what it should
/// have had. Keyed by ruleset and class ID rather than a campaign-roster
/// character, since a class grants the same resources regardless of whether
/// the character wielding it came from the roster or was built by a player.
///
/// Character level is a parameter rather than an assumption because a class
/// grants more as it advances -- and because the validator re-derives this on
/// every session mutation, a level the two disagreed about would reject a
/// legitimately leveled party.
internal static class CampaignResourceGrants
{
    internal static IReadOnlyDictionary<string, int> ForClass(
        string rulesetId,
        string classId,
        int characterLevel)
    {
        if (string.IsNullOrWhiteSpace(rulesetId))
        {
            throw new ArgumentException(
                "Ruleset ID is required.",
                nameof(rulesetId));
        }

        if (string.IsNullOrWhiteSpace(classId))
        {
            throw new ArgumentException(
                "Class ID is required.",
                nameof(classId));
        }

        if (characterLevel < AdvancementRules.MinimumLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterLevel),
                characterLevel,
                "Character level must be at least the minimum level.");
        }

        ClassDefinition? characterClass = RulesetRegistry
            .Resolve(rulesetId)
            .Definition
            .Classes
            .FirstOrDefault(candidate => string.Equals(
                candidate.Id,
                classId,
                StringComparison.Ordinal));

        if (characterClass is null)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        return ResolveSpellSlots(characterClass, characterLevel).ToDictionary(
            slots => SpellSlotResources.ForLevel(slots.Key),
            slots => slots.Value,
            StringComparer.Ordinal);
    }

    /// The slot table in effect at a character level: the highest authored
    /// entry at or below it.
    ///
    /// Content authors only the levels where the table changes, so a level
    /// with no entry of its own keeps the last one that applied rather than
    /// dropping to nothing. A level past everything authored therefore holds
    /// at the last authored table -- the same "content is only written this
    /// far" reality FeaturesByLevel already has, not a silent failure.
    private static IReadOnlyDictionary<int, int> ResolveSpellSlots(
        ClassDefinition characterClass,
        int characterLevel)
    {
        int[] applicableLevels = characterClass.SpellSlotsByCharacterLevel.Keys
            .Where(level => level <= characterLevel)
            .ToArray();

        return applicableLevels.Length == 0
            ? new Dictionary<int, int>()
            : characterClass.SpellSlotsByCharacterLevel[applicableLevels.Max()];
    }
}
