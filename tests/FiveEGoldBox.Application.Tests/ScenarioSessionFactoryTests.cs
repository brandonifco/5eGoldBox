using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class ScenarioSessionFactoryTests
{
    private const int RandomSeed = 8675309;

    [Fact]
    public void CreateNew_ReturnsCanonicalWatchtowerStartingState()
    {
        ApplicationSessionState state =
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                RandomSeed);

        Assert.Equal(
            "scenario.watchtower",
            state.ScenarioId);
        Assert.Equal(
            ApplicationMode.Outpost,
            state.CurrentMode);
        Assert.Equal(
            "location.outpost",
            state.CurrentLocationId);
        Assert.Equal(
            WatchtowerScenarioProgress
                .MissionNotAccepted,
            WatchtowerScenario.ProgressOf(state));
        Assert.Equal(RandomSeed, state.RandomSeed);
        Assert.Equal(0, state.RandomValuesConsumed);
        Assert.Null(state.RegionalTravel);
        Assert.Null(state.Exploration);
        Assert.Null(state.ActiveEncounter);

        Assert.Equal("party.player", state.Party.PartyId);
        Assert.Collection(
            state.Party.Members,
            member => AssertMember(
                member,
                partyMemberId: "party-member.fighter",
                characterDefinitionId:
                    "character.fighter",
                displayName: "Fighter",
                classId: "class.fighter",
                maximumHitPoints: 12,
                currentHitPoints: 8,
                temporaryHitPoints: 2,
                ammunition: null),
            member => AssertMember(
                member,
                partyMemberId: "party-member.rogue",
                characterDefinitionId:
                    "character.rogue",
                displayName: "Rogue",
                classId: "class.rogue",
                maximumHitPoints: 10,
                currentHitPoints: 10,
                temporaryHitPoints: 0,
                ammunition: new AmmunitionState
                {
                    WeaponId = "weapon.shortbow",
                    AmmunitionItemId = "item.arrow",
                    RemainingQuantity = 12
                }),
            member => AssertMember(
                member,
                partyMemberId: "party-member.cleric",
                characterDefinitionId:
                    "character.cleric",
                displayName: "Cleric",
                classId: "class.cleric",
                maximumHitPoints: 10,
                currentHitPoints: 10,
                temporaryHitPoints: 0,
                ammunition: null),
            member => AssertMember(
                member,
                partyMemberId: "party-member.wizard",
                characterDefinitionId:
                    "character.wizard",
                displayName: "Wizard",
                classId: "class.wizard",
                maximumHitPoints: 7,
                currentHitPoints: 7,
                temporaryHitPoints: 0,
                ammunition: null));

        ApplicationSessionRules.Validate(state);
    }

    [Fact]
    public void CreateNew_WithSameSeed_ReturnsValueEquivalentState()
    {
        ApplicationSessionState first =
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                RandomSeed);
        ApplicationSessionState second =
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                RandomSeed);

        AssertStateEquivalent(first, second);
    }

    [Fact]
    public void CreateNew_RepeatedCallsReturnIndependentState()
    {
        ApplicationSessionState first =
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                RandomSeed);
        ApplicationSessionState second =
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                RandomSeed);

        Assert.NotSame(first, second);
        Assert.NotSame(first.Party, second.Party);
        Assert.NotSame(
            first.Party.Members,
            second.Party.Members);

        for (int index = 0;
            index < first.Party.Members.Count;
            index++)
        {
            PartyMemberState firstMember =
                first.Party.Members[index];
            PartyMemberState secondMember =
                second.Party.Members[index];

            Assert.NotSame(firstMember, secondMember);
            Assert.NotSame(
                firstMember.Health,
                secondMember.Health);
            Assert.NotSame(
                firstMember.Health.HitPoints,
                secondMember.Health.HitPoints);
            Assert.NotSame(
                firstMember.Health.DeathSavingThrows,
                secondMember.Health.DeathSavingThrows);
        }

        Assert.NotSame(
            first.Party.Members[CampaignTestParty.ArcherIndex()]
                .Ammunition,
            second.Party.Members[CampaignTestParty.ArcherIndex()]
                .Ammunition);

        PartyMemberState[] changedMembers =
            first.Party.Members.ToArray();
        PartyMemberState fighter = changedMembers[0];

        changedMembers[0] = fighter with
        {
            DisplayName = "Changed Fighter",
            Health = fighter.Health with
            {
                HitPoints = fighter.Health.HitPoints with
                {
                    CurrentHitPoints = 1,
                    TemporaryHitPoints = 0
                }
            }
        };

        ApplicationSessionState changedFirst =
            first with
            {
                Party = first.Party with
                {
                    Members = changedMembers
                }
            };

        Assert.Equal(
            "Changed Fighter",
            changedFirst.Party.Members[0].DisplayName);
        Assert.Equal(
            1,
            changedFirst.Party.Members[0]
                .Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            "Fighter",
            second.Party.Members[0].DisplayName);
        Assert.Equal(
            8,
            second.Party.Members[0]
                .Health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            2,
            second.Party.Members[0]
                .Health.HitPoints.TemporaryHitPoints);
    }

    private static void AssertMember(
        PartyMemberState member,
        string partyMemberId,
        string characterDefinitionId,
        string displayName,
        string classId,
        int maximumHitPoints,
        int currentHitPoints,
        int temporaryHitPoints,
        AmmunitionState? ammunition)
    {
        Assert.Equal(
            partyMemberId,
            member.PartyMemberId);
        Assert.Equal(
            characterDefinitionId,
            member.CharacterDefinitionId);
        Assert.Equal(displayName, member.DisplayName);
        Assert.Equal(classId, member.ClassId);
        Assert.Equal(
            CombatantZeroHitPointPolicy
                .DeathSavingThrows,
            member.ZeroHitPointPolicy);
        AssertHealth(
            member.Health,
            maximumHitPoints,
            currentHitPoints,
            temporaryHitPoints);
        Assert.Equal(ammunition, member.Ammunition);
    }

    private static void AssertHealth(
        CombatantHealthState health,
        int maximumHitPoints,
        int currentHitPoints,
        int temporaryHitPoints)
    {
        Assert.Equal(
            maximumHitPoints,
            health.HitPoints.MaximumHitPoints);
        Assert.Equal(
            currentHitPoints,
            health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            temporaryHitPoints,
            health.HitPoints.TemporaryHitPoints);
        Assert.Equal(
            0,
            health.DeathSavingThrows.SuccessCount);
        Assert.Equal(
            0,
            health.DeathSavingThrows.FailureCount);
        Assert.False(
            health.DeathSavingThrows.IsStable);
        Assert.False(health.IsInstantlyDead);
        Assert.False(health.IsDead);
    }

    private static void AssertStateEquivalent(
        ApplicationSessionState expected,
        ApplicationSessionState actual)
    {
        Assert.Equal(expected.ScenarioId, actual.ScenarioId);
        Assert.Equal(
            expected.CurrentMode,
            actual.CurrentMode);
        Assert.Equal(
            expected.CurrentLocationId,
            actual.CurrentLocationId);
        Assert.Equal(
            expected.Scenario,
            actual.Scenario);
        Assert.Equal(
            expected.RandomSeed,
            actual.RandomSeed);
        Assert.Equal(
            expected.RandomValuesConsumed,
            actual.RandomValuesConsumed);
        Assert.Equal(
            expected.RegionalTravel,
            actual.RegionalTravel);
        Assert.Equal(
            expected.Exploration,
            actual.Exploration);
        Assert.Equal(
            expected.ActiveEncounter,
            actual.ActiveEncounter);
        Assert.Equal(
            expected.Party.PartyId,
            actual.Party.PartyId);

        // A member's Resources is an IReadOnlyList, which record equality
        // compares by reference — two separately built parties never match on
        // it once anybody carries a spell slot. Compare what a member is
        // instead of the instance holding it.
        Assert.Equal(
            expected.Party.Members.Count,
            actual.Party.Members.Count);
        Assert.Equal(
            expected.Party.Members.Select(member => member with
            {
                Resources = Array.Empty<CharacterResourceState>()
            }),
            actual.Party.Members.Select(member => member with
            {
                Resources = Array.Empty<CharacterResourceState>()
            }));
        Assert.Equal(
            expected.Party.Members.SelectMany(
                member => member.Resources),
            actual.Party.Members.SelectMany(
                member => member.Resources));
    }
}
