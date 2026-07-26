using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Tests;

/// The Watchtower scenario's party requirements, which moved out of
/// ApplicationSessionRules so a different scenario can want a different party.
/// The engine still owns whether a party is internally coherent; these cover
/// only what this adventure demands of it.
public sealed class WatchtowerPartyCompositionValidatorTests
{
    [Fact]
    public void Validate_AcceptsTheAuthoredParty()
    {
        WatchtowerPartyCompositionValidator.Validate(CreateParty());
    }

    [Fact]
    public void Validate_RejectsAPartyOfTheWrongSize()
    {
        PartyState party = CreateParty();

        PartyState tooFew = party with
        {
            Members = Array.AsReadOnly(party.Members.Take(2).ToArray())
        };
        PartyState tooMany = party with
        {
            Members = Array.AsReadOnly(
                party.Members.Append(party.Members[0]).ToArray())
        };

        Assert.Throws<ArgumentException>(() =>
            WatchtowerPartyCompositionValidator.Validate(tooFew));
        Assert.Throws<ArgumentException>(() =>
            WatchtowerPartyCompositionValidator.Validate(tooMany));
    }

    [Fact]
    public void Validate_RejectsAClassTheScenarioDidNotAuthor()
    {
        PartyState party = ReplaceFirstMember(
            CreateParty(),
            member => member with { ClassId = "class.wizard" });

        Assert.Throws<ArgumentException>(() =>
            WatchtowerPartyCompositionValidator.Validate(party));
    }

    [Fact]
    public void Validate_RejectsADuplicatedAuthoredClass()
    {
        PartyState party = CreateParty();
        PartyMemberState[] members = party.Members.ToArray();
        members[1] = members[1] with { ClassId = members[0].ClassId };

        Assert.Throws<ArgumentException>(() =>
            WatchtowerPartyCompositionValidator.Validate(
                party with { Members = Array.AsReadOnly(members) }));
    }

    /// Only the Ranger draws on ammunition in this scenario.
    [Fact]
    public void Validate_RejectsAmmunitionHeldByTheWrongMember()
    {
        PartyState party = CreateParty();
        PartyMemberState ranger = Assert.Single(
            party.Members,
            member => member.Ammunition is not null);
        PartyMemberState other = party.Members
            .First(member => member.Ammunition is null);

        PartyState rangerDisarmed = ReplaceMember(
            party,
            ranger.PartyMemberId,
            member => member with { Ammunition = null });
        PartyState otherArmed = ReplaceMember(
            party,
            other.PartyMemberId,
            member => member with { Ammunition = ranger.Ammunition });

        Assert.Throws<ArgumentException>(() =>
            WatchtowerPartyCompositionValidator.Validate(rangerDisarmed));
        Assert.Throws<ArgumentException>(() =>
            WatchtowerPartyCompositionValidator.Validate(otherArmed));
    }

    private static PartyState CreateParty()
    {
        return ScenarioSessionFactory
            .CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                randomSeed: 7)
            .Party;
    }

    private static PartyState ReplaceFirstMember(
        PartyState party,
        Func<PartyMemberState, PartyMemberState> change)
    {
        return ReplaceMember(
            party,
            party.Members[0].PartyMemberId,
            change);
    }

    private static PartyState ReplaceMember(
        PartyState party,
        string partyMemberId,
        Func<PartyMemberState, PartyMemberState> change)
    {
        PartyMemberState[] members = party.Members
            .Select(member => string.Equals(
                    member.PartyMemberId,
                    partyMemberId,
                    StringComparison.Ordinal)
                ? change(member)
                : member)
            .ToArray();

        return party with { Members = Array.AsReadOnly(members) };
    }
}
