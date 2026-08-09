using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Encounters;

internal static class PartyEncounterMapper
{
    internal static PartyEncounterParticipant CreateParticipant(
        PartyMemberState member,
        ValidatedRuleset ruleset,
        string sideId,
        GridPosition startingPosition,
        int level)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(ruleset);

        CharacterDraft draft =
            CampaignCharacterDraftFactory.CreateDraft(
                member,
                CampaignRegistry.Resolve(
                    FrontierCampaignIds.CampaignId),
                ruleset,
                level);
        CharacterSnapshot snapshot =
            new CharacterResolver(ruleset).Resolve(draft);

        int maximumHitPoints = snapshot.MaxHitPoints
            ?? throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' did not resolve maximum hit points.");
        int armorClass = snapshot.ArmorClass
            ?? throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' did not resolve armor class.");
        int movementSpeedFeet = snapshot.SpeedFeet
            ?? throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' did not resolve movement speed.");

        if (maximumHitPoints
            != member.Health.HitPoints.MaximumHitPoints)
        {
            throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' does not match the persistent maximum hit points.");
        }

        if (snapshot.WeaponAttacks.Count == 0)
        {
            throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' did not resolve a weapon attack.");
        }

        if (snapshot.SavingThrowBonuses.Count
            != Enum.GetValues<Ability>().Length)
        {
            throw new InvalidOperationException(
                $"Character definition '{member.CharacterDefinitionId}' did not resolve every saving-throw ability.");
        }

        if (member.Ammunition is not null)
        {
            WeaponAttack ammunitionWeapon =
                snapshot.WeaponAttacks.SingleOrDefault(
                    attack => string.Equals(
                        attack.WeaponId,
                        member.Ammunition.WeaponId,
                        StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    "The persistent ammunition weapon was not resolved for the Ranger.");

            if (!string.Equals(
                ammunitionWeapon.AmmunitionItemId,
                member.Ammunition.AmmunitionItemId,
                StringComparison.Ordinal)
                || ammunitionWeapon
                    .AmmunitionQuantityAvailable
                    != member.Ammunition.RemainingQuantity)
            {
                throw new InvalidOperationException(
                    "The resolved Ranger ammunition does not match persistent party state.");
            }
        }

        EncounterParticipantSetup setup = new()
        {
            Combatant = new CombatantState
            {
                CombatantId = member.PartyMemberId,
                ZeroHitPointPolicy =
                    member.ZeroHitPointPolicy,
                Health = member.Health
            },
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = armorClass,
                WeaponAttacks = snapshot.WeaponAttacks,
                SpellAttacks = snapshot.SpellAttacks,
                Contributions =
                    snapshot.Contributions,
                Resources = ToCombatantResources(member),
                SavingThrowBonuses =
                    snapshot.SavingThrowBonuses,
                DamageResponses =
                    snapshot.DamageResponses
            },
            SideId = sideId,
            MovementSpeedFeet = movementSpeedFeet,
            StartingPosition = startingPosition
        };

        return new PartyEncounterParticipant
        {
            Setup = setup,
            InitiativeBonus = snapshot.InitiativeBonus
        };
    }

    /// What the character persistently holds becomes what the encounter may
    /// spend, exactly as ammunition does — and what is left comes back out
    /// afterwards. A caster who never reaches the battlefield with its slots
    /// has spell slots only in the sense that a save file says so.
    private static IReadOnlyList<CombatantResource> ToCombatantResources(
        PartyMemberState member)
    {
        return member.Resources
            .Select(resource => new CombatantResource
            {
                ResourceId = resource.ResourceId,
                Remaining = resource.Remaining,
                Maximum = resource.Maximum
            })
            .ToArray();
    }
}

internal sealed record PartyEncounterParticipant
{
    internal required EncounterParticipantSetup Setup
    { get; init; }

    internal required int InitiativeBonus { get; init; }
}
