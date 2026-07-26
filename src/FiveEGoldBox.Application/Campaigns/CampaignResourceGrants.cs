using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Campaigns;

/// What a character's build grants it to spend.
///
/// One place answers this, so the factory that fills a character's resources
/// and the validator that checks them cannot disagree about what it should
/// have had.
internal static class CampaignResourceGrants
{
    internal static IReadOnlyDictionary<string, int> ForCharacter(
        CampaignDefinition campaign,
        CampaignCharacterDefinition character)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(character);

        ClassDefinition? characterClass = RulesetRegistry
            .Resolve(campaign.RulesetId)
            .Definition
            .Classes
            .FirstOrDefault(candidate => string.Equals(
                candidate.Id,
                character.ClassId,
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
