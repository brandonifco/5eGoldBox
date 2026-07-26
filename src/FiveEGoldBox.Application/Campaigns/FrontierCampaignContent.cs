using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Campaigns;

/// The campaign these scenarios belong to, as authored data.
///
/// Every value here was previously a literal in a C# method — one method per
/// character, in a class named after a scenario. They are the same values;
/// what changed is that a roster is now something a campaign declares rather
/// than something the first adventure happened to own.
///
/// The roster is three characters because that is what exists. The scope
/// baseline commits to four active plus a reserve, and moving to it is a
/// content edit here rather than a code change anywhere — which was the point
/// of writing it down this way.
internal static class FrontierCampaignContent
{
    internal const string CampaignId = "campaign.frontier";

    private const string PartyId = "party.player";

    private const string HumanRaceId = "race.human";

    private const string SoldierBackgroundId = "background.soldier";

    internal static string StartingPartyId => PartyId;

    internal static CampaignDefinition CreateDefinition()
    {
        return new CampaignDefinition
        {
            CampaignId = CampaignId,
            DisplayName = "The Frontier Commission",
            RulesetId = RulesetRegistry.CampaignRulesetId,
            ActivePartySize = 3,
            Roster =
            [
                CreateFighter(),
                CreateBarbarian(),
                CreateRanger()
            ],
            ScenarioIds =
            [
                WatchtowerScenarioContent.ScenarioId,
                SunkenChapelScenarioDefinitionProvider.ScenarioId
            ]
        };
    }

    private static CampaignCharacterDefinition CreateFighter()
    {
        return new CampaignCharacterDefinition
        {
            PartyMemberId = "party-member.fighter",
            CharacterDefinitionId = "character.fighter",
            DisplayName = "Fighter",
            RaceId = HumanRaceId,
            ClassId = "class.fighter",
            BackgroundId = SoldierBackgroundId,
            AbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 11,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 7,
                [Ability.Wisdom] = 9,
                [Ability.Charisma] = 12
            },
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ],
            EquippedWeaponIds = ["weapon.longsword"],
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            MaximumHitPoints = 12,
            CurrentHitPoints = 8,
            TemporaryHitPoints = 2
        };
    }

    private static CampaignCharacterDefinition CreateBarbarian()
    {
        return new CampaignCharacterDefinition
        {
            PartyMemberId = "party-member.barbarian",
            CharacterDefinitionId = "character.barbarian",
            DisplayName = "Barbarian",
            RaceId = HumanRaceId,
            ClassId = "class.barbarian",
            BackgroundId = SoldierBackgroundId,
            AbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 13,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 7,
                [Ability.Wisdom] = 11,
                [Ability.Charisma] = 9
            },
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.survival"
            ],
            EquippedWeaponIds = ["weapon.greataxe"],
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            MaximumHitPoints = 14,
            CurrentHitPoints = 14
        };
    }

    private static CampaignCharacterDefinition CreateRanger()
    {
        return new CampaignCharacterDefinition
        {
            PartyMemberId = "party-member.ranger",
            CharacterDefinitionId = "character.ranger",
            DisplayName = "Ranger",
            RaceId = HumanRaceId,
            ClassId = "class.ranger",
            BackgroundId = SoldierBackgroundId,
            AbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 11,
                [Ability.Dexterity] = 15,
                [Ability.Constitution] = 11,
                [Ability.Intelligence] = 9,
                [Ability.Wisdom] = 15,
                [Ability.Charisma] = 7
            },
            SelectedSkillIds =
            [
                "skill.perception",
                "skill.stealth",
                "skill.survival"
            ],
            EquippedWeaponIds = ["weapon.longbow"],
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            MaximumHitPoints = 11,
            CurrentHitPoints = 11,
            Ammunition = new CampaignAmmunitionDefinition
            {
                WeaponId = "weapon.longbow",
                AmmunitionItemId = "item.arrow",
                Quantity = 7
            }
        };
    }
}
