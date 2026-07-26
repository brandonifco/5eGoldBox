using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Campaigns;

/// One member of a campaign's roster, as authored data.
///
/// This is the build a player would have made in town: race, class,
/// background, the scores they rolled and the skills and gear they chose. The
/// engine resolves it through the same character pipeline any character goes
/// through — nothing here bypasses that.
internal sealed record CampaignCharacterDefinition
{
    internal required string PartyMemberId { get; init; }

    /// Identifies the build. Persists in saves, so it outlives any renaming.
    internal required string CharacterDefinitionId { get; init; }

    internal required string DisplayName { get; init; }

    internal required string RaceId { get; init; }

    internal required string ClassId { get; init; }

    internal required string BackgroundId { get; init; }

    internal required IReadOnlyDictionary<Ability, int> AbilityScores { get; init; }

    internal required IReadOnlyList<string> SelectedSkillIds { get; init; }

    internal required IReadOnlyList<string> EquippedWeaponIds { get; init; }

    internal required CombatantZeroHitPointPolicy ZeroHitPointPolicy { get; init; }

    /// What the character starts the campaign with. Maximum hit points are
    /// derived from class and Constitution by the character pipeline; this
    /// states them too so a save can be checked against the build it claims.
    internal required int MaximumHitPoints { get; init; }

    internal required int CurrentHitPoints { get; init; }

    internal int TemporaryHitPoints { get; init; }

    /// Null for characters who carry nothing that runs out.
    internal CampaignAmmunitionDefinition? Ammunition { get; init; }
}
