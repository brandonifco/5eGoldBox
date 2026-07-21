using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerScenarioSessionFactoryTests
{
    private const int RandomSeed = 8675309;

    [Fact]
    public void CreateNew_ReturnsCanonicalWatchtowerStartingState()
    {
        ApplicationSessionState state =
            WatchtowerScenarioSessionFactory.CreateNew(
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
            state.Scenario.Progress);
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
                partyMemberId:
                    "party-member.barbarian",
                characterDefinitionId:
                    "character.barbarian",
                displayName: "Barbarian",
                classId: "class.barbarian",
                maximumHitPoints: 14,
                currentHitPoints: 14,
                temporaryHitPoints: 0,
                ammunition: null),
            member => AssertMember(
                member,
                partyMemberId: "party-member.ranger",
                characterDefinitionId:
                    "character.ranger",
                displayName: "Ranger",
                classId: "class.ranger",
                maximumHitPoints: 11,
                currentHitPoints: 11,
                temporaryHitPoints: 0,
                ammunition: new AmmunitionState
                {
                    WeaponId = "weapon.longbow",
                    AmmunitionItemId = "item.arrow",
                    RemainingQuantity = 7
                }));

        ApplicationSessionRules.Validate(state);
    }

    [Fact]
    public void CreateNew_WithSameSeed_ReturnsValueEquivalentState()
    {
        ApplicationSessionState first =
            WatchtowerScenarioSessionFactory.CreateNew(
                RandomSeed);
        ApplicationSessionState second =
            WatchtowerScenarioSessionFactory.CreateNew(
                RandomSeed);

        AssertStateEquivalent(first, second);
    }

    [Fact]
    public void CreateNew_RepeatedCallsReturnIndependentState()
    {
        ApplicationSessionState first =
            WatchtowerScenarioSessionFactory.CreateNew(
                RandomSeed);
        ApplicationSessionState second =
            WatchtowerScenarioSessionFactory.CreateNew(
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
            first.Party.Members[2].Ammunition,
            second.Party.Members[2].Ammunition);

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
        Assert.Equal(
            expected.Party.Members,
            actual.Party.Members);
    }
}
