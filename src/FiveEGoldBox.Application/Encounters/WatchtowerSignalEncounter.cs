using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Encounters;

internal static class WatchtowerSignalEncounter
{
    internal const string EncounterId =
        "encounter.watchtower-signal-ambush";

    internal const string BattlefieldId =
        "battlefield.watchtower-signal-chamber";

    internal const string PartySideId =
        "side.party";

    internal const string RaiderSideId =
        "side.watchtower-raiders";

    internal const string MeleeRaiderId =
        "combatant.watchtower-raider.melee";

    internal const string RangedRaiderId =
        "combatant.watchtower-raider.ranged";

    internal const string MeleeRaiderWeaponId =
        "weapon.watchtower-raider.scimitar";

    internal const string RangedRaiderWeaponId =
        "weapon.watchtower-raider.shortbow";

    internal const string RangedRaiderAmmunitionItemId = "item.arrow";

    internal const int RangedRaiderAmmunitionQuantity = 12;

    internal const int BattlefieldWidth = 9;

    internal const int BattlefieldHeight = 8;

    internal static readonly IReadOnlyList<GridPosition>
        PartyStartingPositions =
        [
            new GridPosition(1, 1),
            new GridPosition(1, 2),
            new GridPosition(0, 2),
            new GridPosition(0, 1)
        ];

    internal static readonly GridPosition
        MeleeRaiderStartingPosition = new(2, 1);

    internal static readonly GridPosition
        RangedRaiderStartingPosition = new(4, 2);

    internal static bool IsAuthoredParticipantId(
        string combatantId,
        PartyState party)
    {
        ArgumentNullException.ThrowIfNull(party);

        return party.Members.Any(member =>
                string.Equals(
                    member.PartyMemberId,
                    combatantId,
                    StringComparison.Ordinal))
            || string.Equals(
                combatantId,
                MeleeRaiderId,
                StringComparison.Ordinal)
            || string.Equals(
                combatantId,
                RangedRaiderId,
                StringComparison.Ordinal);
    }

}
