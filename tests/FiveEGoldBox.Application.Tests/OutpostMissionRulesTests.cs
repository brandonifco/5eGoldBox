using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class OutpostMissionRulesTests
{
    [Fact]
    public void Resolve_WithAcceptMission_AdvancesProgressAndReportsResult()
    {
        ApplicationSessionState session =
            CreateValidSession();

        OutpostMissionResult result =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission);

        Assert.Equal(
            OutpostMissionChoice.AcceptMission,
            result.Choice);
        Assert.True(result.DidProgressChange);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            result.State.Scenario.Progress);
        Assert.Equal(
            ApplicationMode.Outpost,
            result.State.CurrentMode);
    }

    [Fact]
    public void Resolve_WithAcceptMission_PreservesUnrelatedState()
    {
        ApplicationSessionState session =
            CreateValidSession();

        ApplicationSessionState accepted =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission)
                .State;

        Assert.Equal(session.ScenarioId, accepted.ScenarioId);
        Assert.Equal(
            session.CurrentLocationId,
            accepted.CurrentLocationId);
        Assert.Equal(
            session.RandomSeed,
            accepted.RandomSeed);
        Assert.Equal(
            session.RandomValuesConsumed,
            accepted.RandomValuesConsumed);
        AssertPartyEquivalent(
            session.Party,
            accepted.Party);
    }

    [Fact]
    public void Resolve_WithAcceptMission_DoesNotMutateInputSession()
    {
        ApplicationSessionState session =
            CreateValidSession();

        _ = OutpostMissionRules.Resolve(
            session,
            OutpostMissionChoice.AcceptMission);

        Assert.Equal(
            WatchtowerScenarioProgress
                .MissionNotAccepted,
            session.Scenario.Progress);
        Assert.Equal(
            ApplicationMode.Outpost,
            session.CurrentMode);
        Assert.Equal(12, session.RandomValuesConsumed);
    }

    [Fact]
    public void Resolve_WithNotYet_LeavesSessionSemanticallyUnchanged()
    {
        ApplicationSessionState session =
            CreateValidSession();

        OutpostMissionResult result =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.NotYet);

        Assert.Equal(
            OutpostMissionChoice.NotYet,
            result.Choice);
        Assert.False(result.DidProgressChange);
        AssertSessionEquivalent(
            session,
            result.State);
    }

    [Fact]
    public void Resolve_WithNotYet_AllowsLaterMissionAcceptance()
    {
        ApplicationSessionState session =
            CreateValidSession();
        ApplicationSessionState deferred =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.NotYet)
                .State;

        OutpostMissionResult accepted =
            OutpostMissionRules.Resolve(
                deferred,
                OutpostMissionChoice.AcceptMission);

        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            accepted.State.Scenario.Progress);
        Assert.True(accepted.DidProgressChange);
    }

    [Fact]
    public void Resolve_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OutpostMissionRules.Resolve(
                null!,
                OutpostMissionChoice.AcceptMission));
    }

    [Fact]
    public void Resolve_WithUndefinedChoice_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OutpostMissionRules.Resolve(
                session,
                (OutpostMissionChoice)999));
    }

    [Fact]
    public void Resolve_WithInvalidSession_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession() with
            {
                ScenarioId = " "
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission));
    }

    [Fact]
    public void Resolve_WithNullScenario_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession() with
            {
                Scenario = null!
            };

        Assert.Throws<ArgumentNullException>(() =>
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission));
    }

    [Fact]
    public void Resolve_WhenModeIsNotOutpost_Throws()
    {
        ApplicationSessionState session =
            CreateValidSession() with
            {
                CurrentMode = ApplicationMode.Exploration
            };

        Assert.ThrowsAny<ArgumentException>(() =>
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission));
    }

    [Theory]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.MissionAccepted)]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.SignalActivated)]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.RaidersDefeated)]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.SuccessReported)]
    [InlineData(
        OutpostMissionChoice.AcceptMission,
        WatchtowerScenarioProgress.ScenarioCompleted)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.MissionAccepted)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.SignalActivated)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.RaidersDefeated)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.SuccessReported)]
    [InlineData(
        OutpostMissionChoice.NotYet,
        WatchtowerScenarioProgress.ScenarioCompleted)]
    public void Resolve_AfterMissionDecisionAvailability_Throws(
        OutpostMissionChoice choice,
        WatchtowerScenarioProgress progress)
    {
        ApplicationSessionState session =
            CreateValidSession() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress = progress
                }
            };

        Assert.Throws<InvalidOperationException>(() =>
            OutpostMissionRules.Resolve(
                session,
                choice));
    }

    private static void AssertSessionEquivalent(
        ApplicationSessionState expected,
        ApplicationSessionState actual)
    {
        Assert.Equal(expected.ScenarioId, actual.ScenarioId);
        Assert.Equal(expected.CurrentMode, actual.CurrentMode);
        Assert.Equal(
            expected.CurrentLocationId,
            actual.CurrentLocationId);
        Assert.Equal(
            expected.Scenario.Progress,
            actual.Scenario.Progress);
        Assert.Equal(expected.RandomSeed, actual.RandomSeed);
        Assert.Equal(
            expected.RandomValuesConsumed,
            actual.RandomValuesConsumed);
        AssertPartyEquivalent(
            expected.Party,
            actual.Party);
    }

    private static void AssertPartyEquivalent(
        PartyState expected,
        PartyState actual)
    {
        Assert.Equal(expected.PartyId, actual.PartyId);
        Assert.Equal(
            expected.Members.Count,
            actual.Members.Count);

        for (int index = 0;
            index < expected.Members.Count;
            index++)
        {
            PartyMemberState expectedMember =
                expected.Members[index];
            PartyMemberState actualMember =
                actual.Members[index];

            Assert.Equal(
                expectedMember.PartyMemberId,
                actualMember.PartyMemberId);
            Assert.Equal(
                expectedMember.CharacterDefinitionId,
                actualMember.CharacterDefinitionId);
            Assert.Equal(
                expectedMember.DisplayName,
                actualMember.DisplayName);
            Assert.Equal(
                expectedMember.ClassId,
                actualMember.ClassId);
            Assert.Equal(
                expectedMember.ZeroHitPointPolicy,
                actualMember.ZeroHitPointPolicy);
            Assert.Equal(
                expectedMember.Health,
                actualMember.Health);
            Assert.Equal(
                expectedMember.Ammunition,
                actualMember.Ammunition);
        }
    }

    private static ApplicationSessionState
        CreateValidSession()
    {
        PartyMemberState[] members =
        [
            CreateMember(
                partyMemberId:
                    "party-member.fighter",
                characterDefinitionId:
                    "character.fighter",
                displayName: "Fighter",
                classId: "class.fighter",
                maximumHitPoints: 12) with
            {
                Health = CombatantHealthRules.Create(
                    maximumHitPoints: 12) with
                {
                    HitPoints = new HitPointState
                    {
                        MaximumHitPoints = 12,
                        CurrentHitPoints = 8,
                        TemporaryHitPoints = 2
                    }
                }
            },
            CreateMember(
                partyMemberId:
                    "party-member.barbarian",
                characterDefinitionId:
                    "character.barbarian",
                displayName: "Barbarian",
                classId: "class.barbarian",
                maximumHitPoints: 14),
            CreateMember(
                partyMemberId:
                    "party-member.ranger",
                characterDefinitionId:
                    "character.ranger",
                displayName: "Ranger",
                classId: "class.ranger",
                maximumHitPoints: 11) with
            {
                Ammunition = new AmmunitionState
                {
                    WeaponId = "weapon.longbow",
                    AmmunitionItemId = "item.arrow",
                    RemainingQuantity = 7
                }
            }
        ];

        ApplicationSessionState session =
            ApplicationSessionRules.CreateNew(
                scenarioId: "scenario.watchtower",
                currentLocationId: "location.outpost",
                party: new PartyState
                {
                    PartyId = "party.player",
                    Members = members
                },
                randomSeed: 8675309);

        return session with
        {
            RandomValuesConsumed = 12
        };
    }

    private static PartyMemberState CreateMember(
        string partyMemberId,
        string characterDefinitionId,
        string displayName,
        string classId,
        int maximumHitPoints)
    {
        return new PartyMemberState
        {
            PartyMemberId = partyMemberId,
            CharacterDefinitionId =
                characterDefinitionId,
            DisplayName = displayName,
            ClassId = classId,
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows,
            Health = CombatantHealthRules.Create(
                maximumHitPoints),
            Ammunition = null
        };
    }
}
