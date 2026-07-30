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
/// The roster is the scope baseline's four active characters, with the two
/// this campaign started life with kept on as reserve. It became a content
/// edit rather than a code change the moment the campaign concept landed,
/// which was the point of writing it down this way — what it still costs is
/// the two frozen combat transcripts, because a different party rolls
/// different dice in a different order.
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
            ActivePartySize = 4,

            // The first four take the field; the rest are reserve. Nothing
            // swaps them yet, and the distinction is the campaign's to make
            // rather than a scenario's.
            Roster =
            [
                CreateFighter(),
                CreateRogue(),
                CreateCleric(),
                CreateWizard(),
                CreateBarbarian(),
                CreateRanger()
            ],
            ScenarioIds =
            [
                WatchtowerScenarioContent.ScenarioId,
                SunkenChapelScenarioIds.ScenarioId,
                HollowMillScenarioIds.ScenarioId
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

    /// Carries the campaign's second bow, which is what finally makes the
    /// post-combat ammunition projection testable: it has been generic since
    /// PR #102 and until now the only bow belonged to the only character who
    /// could hold one.
    ///
    /// Carries the dagger too, now that the decision surface offers a
    /// combatant every weapon it carries rather than exactly one. Either
    /// satisfies Sneak Attack — a ranged weapon and a finesse one both do —
    /// so the choice between them is purely about range and ammunition, the
    /// way it would be for a real rogue at the table.
    private static CampaignCharacterDefinition CreateRogue()
    {
        return new CampaignCharacterDefinition
        {
            PartyMemberId = "party-member.rogue",
            CharacterDefinitionId = "character.rogue",
            DisplayName = "Rogue",
            RaceId = HumanRaceId,
            ClassId = CampaignRulesetIds.RogueClassId,
            BackgroundId = SoldierBackgroundId,
            AbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 9,
                [Ability.Dexterity] = 15,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 12,
                [Ability.Wisdom] = 11,
                [Ability.Charisma] = 7
            },
            SelectedSkillIds =
            [
                "skill.perception",
                "skill.stealth"
            ],
            EquippedWeaponIds =
            [
                CampaignRulesetIds.RogueWeaponId,
                CampaignRulesetIds.RogueSidearmWeaponId
            ],
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            MaximumHitPoints = 10,
            CurrentHitPoints = 10,
            Ammunition = new CampaignAmmunitionDefinition
            {
                WeaponId = CampaignRulesetIds.RogueWeaponId,
                AmmunitionItemId = "item.arrow",
                Quantity = 12
            }
        };
    }

    /// Prepares one of each thing a cleric is for: something to do at will,
    /// two ways to put hit points back, and the blessing that made the
    /// roll-contribution seam necessary.
    private static CampaignCharacterDefinition CreateCleric()
    {
        return new CampaignCharacterDefinition
        {
            PartyMemberId = "party-member.cleric",
            CharacterDefinitionId = "character.cleric",
            DisplayName = "Cleric",
            RaceId = HumanRaceId,
            ClassId = CampaignRulesetIds.ClericClassId,
            BackgroundId = SoldierBackgroundId,
            AbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 11,
                [Ability.Dexterity] = 9,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 7,
                [Ability.Wisdom] = 15,
                [Ability.Charisma] = 12
            },
            SelectedSkillIds =
            [
                "skill.perception",
                "skill.survival"
            ],
            EquippedWeaponIds =
            [
                CampaignRulesetIds.ClericWeaponId
            ],
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            MaximumHitPoints = 10,
            CurrentHitPoints = 10,
            PreparedSpellIds =
            [
                CampaignRulesetIds.SacredFlameId,
                CampaignRulesetIds.CureWoundsId,
                CampaignRulesetIds.HealingWordId,
                CampaignRulesetIds.BlessId
            ]
        };
    }

    /// Fewest hit points and the only character who would rather never be
    /// reached, which is the whole reason the party has a front line.
    private static CampaignCharacterDefinition CreateWizard()
    {
        return new CampaignCharacterDefinition
        {
            PartyMemberId = "party-member.wizard",
            CharacterDefinitionId = "character.wizard",
            DisplayName = "Wizard",
            RaceId = HumanRaceId,
            ClassId = CampaignRulesetIds.WizardClassId,
            BackgroundId = SoldierBackgroundId,
            AbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 7,
                [Ability.Dexterity] = 13,
                [Ability.Constitution] = 11,
                [Ability.Intelligence] = 15,
                [Ability.Wisdom] = 12,
                [Ability.Charisma] = 9
            },
            SelectedSkillIds =
            [
                "skill.perception",
                "skill.stealth"
            ],
            EquippedWeaponIds =
            [
                CampaignRulesetIds.RogueSidearmWeaponId
            ],
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            MaximumHitPoints = 7,
            CurrentHitPoints = 7,
            PreparedSpellIds =
            [
                CampaignRulesetIds.FireBoltId,
                CampaignRulesetIds.MagicMissileId
            ]
        };
    }

    /// Reserve from here down. These two were the campaign's original party;
    /// they keep their builds so a save naming them still describes somebody
    /// the campaign knows.
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
