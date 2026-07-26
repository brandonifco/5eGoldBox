using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios;

internal static class WatchtowerScenarioContent
{
    internal const string ScenarioId =
        "scenario.watchtower";

    internal const string OutpostLocationId =
        "location.outpost";

    private const string PartyId =
        "party.player";

    private const string FighterPartyMemberId =
        "party-member.fighter";

    private const string BarbarianPartyMemberId =
        "party-member.barbarian";

    private const string RangerPartyMemberId =
        "party-member.ranger";

    private const string HumanRaceId =
        "race.human";

    private const string SoldierBackgroundId =
        "background.soldier";

    internal static PartyState CreateStartingParty()
    {
        return new PartyState
        {
            PartyId = PartyId,
            Members =
            [
                CreatePartyMember(
                    FighterPartyMemberId,
                    WatchtowerPartyDefinitions
                        .FighterDefinitionId,
                    "Fighter",
                    WatchtowerPartyDefinitions
                        .FighterClassId,
                    maximumHitPoints: 12,
                    currentHitPoints: 8,
                    temporaryHitPoints: 2,
                    ammunition: null),
                CreatePartyMember(
                    BarbarianPartyMemberId,
                    WatchtowerPartyDefinitions
                        .BarbarianDefinitionId,
                    "Barbarian",
                    WatchtowerPartyDefinitions
                        .BarbarianClassId,
                    maximumHitPoints: 14,
                    currentHitPoints: 14,
                    temporaryHitPoints: 0,
                    ammunition: null),
                CreatePartyMember(
                    RangerPartyMemberId,
                    WatchtowerPartyDefinitions
                        .RangerDefinitionId,
                    "Ranger",
                    WatchtowerPartyDefinitions
                        .RangerClassId,
                    maximumHitPoints: 11,
                    currentHitPoints: 11,
                    temporaryHitPoints: 0,
                    ammunition: new AmmunitionState
                    {
                        WeaponId =
                            WatchtowerPartyDefinitions
                                .RangerWeaponId,
                        AmmunitionItemId =
                            WatchtowerPartyDefinitions
                                .RangerAmmunitionItemId,
                        RemainingQuantity = 7
                    })
            ]
        };
    }

    /// The campaign's rules, which this scenario shares with every other
    /// scenario in the campaign.
    internal static ValidatedRuleset CreateRuleset()
    {
        return RulesetRegistry.Resolve(
            RulesetRegistry.CampaignRulesetId);
    }

    private static PartyMemberState CreatePartyMember(
        string partyMemberId,
        string characterDefinitionId,
        string displayName,
        string classId,
        int maximumHitPoints,
        int currentHitPoints,
        int temporaryHitPoints,
        AmmunitionState? ammunition)
    {
        return new PartyMemberState
        {
            PartyMemberId = partyMemberId,
            CharacterDefinitionId = characterDefinitionId,
            DisplayName = displayName,
            ClassId = classId,
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows,
            Health = new CombatantHealthState
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints =
                        maximumHitPoints,
                    CurrentHitPoints =
                        currentHitPoints,
                    TemporaryHitPoints =
                        temporaryHitPoints
                },
                DeathSavingThrows =
                    new DeathSavingThrowState
                    {
                        SuccessCount = 0,
                        FailureCount = 0,
                        IsStable = false
                    },
                IsInstantlyDead = false
            },
            Ammunition = ammunition
        };
    }

}
