using FiveEGoldBox.Application.Encounters;

namespace FiveEGoldBox.Application.Parties;

/// The Watchtower scenario's requirements for the party that plays it: three
/// members, one each of the authored classes, with only the Ranger carrying
/// ammunition.
///
/// These are scenario requirements, not engine rules. The engine validates that
/// a party is internally coherent — identities unique, health sane, ammunition
/// well-formed where present — and leaves the question of who may attempt this
/// adventure to the adventure. Which is why a different scenario can want a
/// different party without touching ApplicationSessionRules.
internal static class WatchtowerPartyCompositionValidator
{
    private const int RequiredMemberCount = 3;

    internal static void Validate(
        PartyState party)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(party.Members);

        if (party.Members.Count != RequiredMemberCount)
        {
            throw new ArgumentException(
                $"The bounded party must contain exactly {RequiredMemberCount} members.",
                nameof(party));
        }

        int fighterCount = 0;
        int barbarianCount = 0;
        int rangerCount = 0;

        foreach (PartyMemberState member in party.Members)
        {
            ArgumentNullException.ThrowIfNull(member);

            switch (member.ClassId)
            {
                case WatchtowerPartyDefinitions.FighterClassId:
                    fighterCount++;
                    break;
                case WatchtowerPartyDefinitions.BarbarianClassId:
                    barbarianCount++;
                    break;
                case WatchtowerPartyDefinitions.RangerClassId:
                    rangerCount++;
                    break;
                default:
                    throw new ArgumentException(
                        "The bounded party supports only Fighter, Barbarian, and Ranger class IDs.",
                        nameof(party));
            }

            ValidateAmmunitionOwnership(member);
        }

        if (fighterCount != 1
            || barbarianCount != 1
            || rangerCount != 1)
        {
            throw new ArgumentException(
                "The bounded party must contain one Fighter, one Barbarian, and one Ranger.",
                nameof(party));
        }
    }

    /// Only the Ranger draws on ammunition in this scenario. The engine still
    /// owns whether an ammunition record is itself well-formed.
    private static void ValidateAmmunitionOwnership(
        PartyMemberState member)
    {
        if (member.ClassId == WatchtowerPartyDefinitions.RangerClassId)
        {
            if (member.Ammunition is null)
            {
                throw new ArgumentException(
                    "The Ranger must have ammunition state.",
                    nameof(member));
            }

            return;
        }

        if (member.Ammunition is not null)
        {
            throw new ArgumentException(
                "Only the Ranger may have ammunition state in the bounded party.",
                nameof(member));
        }
    }
}
