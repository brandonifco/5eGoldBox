using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Campaigns;

/// What a character's build grants it to spend.
///
/// One place answers this, so the factory that fills a character's resources
/// and the validator that checks them cannot disagree about what it should
/// have had. Keyed by ruleset and class ID rather than a campaign-roster
/// character, since a class grants the same resources regardless of whether
/// the character wielding it came from the roster or was built by a player.
internal static class CampaignResourceGrants
{
    internal static IReadOnlyDictionary<string, int> ForClass(
        string rulesetId,
        string classId)
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

        return characterClass.SpellSlotsByLevel.ToDictionary(
            slots => SpellSlotResources.ForLevel(slots.Key),
            slots => slots.Value,
            StringComparer.Ordinal);
    }
}
