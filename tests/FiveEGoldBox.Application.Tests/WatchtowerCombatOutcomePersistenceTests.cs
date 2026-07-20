using System.Text.Json.Nodes;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerCombatOutcomePersistenceTests
{
    [Fact]
    public void SerializeAndDeserialize_PostVictoryExploration_PreservesOutcomeState()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeRules.Finalize(
                WatchtowerCombatOutcomeTestData
                    .CreatePartyVictorySession(
                        rangerAmmunition: 2))
                .State;

        ApplicationSessionState loaded = RoundTrip(source);

        Assert.Equal(ApplicationMode.Exploration, loaded.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.RaidersDefeated,
            loaded.Scenario.Progress);
        Assert.Equal(source.CurrentLocationId, loaded.CurrentLocationId);
        Assert.Equal(source.Exploration, loaded.Exploration);
        Assert.Null(loaded.ActiveEncounter);
        Assert.Null(loaded.RegionalTravel);
        AssertPersistentOutcome(source, loaded);
    }

    [Fact]
    public void SerializeAndDeserialize_PartyDefeatedConclusion_PreservesOutcomeState()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeRules.Finalize(
                WatchtowerCombatOutcomeTestData
                    .CreateRaiderVictorySession(
                        rangerAmmunition: 1))
                .State;

        ApplicationSessionState loaded = RoundTrip(source);

        Assert.Equal(
            ApplicationMode.ScenarioConclusion,
            loaded.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.PartyDefeated,
            loaded.Scenario.Progress);
        Assert.Equal(source.CurrentLocationId, loaded.CurrentLocationId);
        Assert.Null(loaded.Exploration);
        Assert.Null(loaded.ActiveEncounter);
        Assert.Null(loaded.RegionalTravel);
        AssertPersistentOutcome(source, loaded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Serialize_OutcomeModes_ContinueUsingFormatVersionOne(
        bool partyVictory)
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeRules.Finalize(
                partyVictory
                    ? WatchtowerCombatOutcomeTestData
                        .CreatePartyVictorySession()
                    : WatchtowerCombatOutcomeTestData
                        .CreateRaiderVictorySession())
                .State;
        JsonNode root = JsonNode.Parse(
            ManualSaveSerializer.Serialize(source))!;

        Assert.Equal(
            ManualSaveSerializer.SupportedFormatVersion,
            root["FormatVersion"]!.GetValue<int>());
        Assert.Equal(1, root["FormatVersion"]!.GetValue<int>());
    }

    [Fact]
    public void Deserialize_ScenarioConclusionWithInvalidShape_ReturnsInvalidSessionState()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeRules.Finalize(
                WatchtowerCombatOutcomeTestData
                    .CreateRaiderVictorySession())
                .State;
        JsonNode root = JsonNode.Parse(
            ManualSaveSerializer.Serialize(source))!;
        root["Session"]!["CurrentLocationId"] =
            "location.outpost";

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                root.ToJsonString());

        Assert.False(result.IsSuccess);
        Assert.Null(result.Session);
        Assert.Equal(
            ManualSaveLoadFailureReason.InvalidSessionState,
            result.FailureReason);
    }

    [Fact]
    public void Serialize_CompletedButUnreconciledEncounter_RemainsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            ManualSaveSerializer.Serialize(
                WatchtowerCombatOutcomeTestData
                    .CreatePartyVictorySession()));
    }

    [Fact]
    public void Serialize_ActiveEncounter_RemainsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            ManualSaveSerializer.Serialize(
                WatchtowerCombatOutcomeTestData
                    .CreateActiveSession()));
    }

    private static ApplicationSessionState RoundTrip(
        ApplicationSessionState source)
    {
        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                ManualSaveSerializer.Serialize(source));

        Assert.True(result.IsSuccess);
        return Assert.IsType<ApplicationSessionState>(
            result.Session);
    }

    private static void AssertPersistentOutcome(
        ApplicationSessionState expected,
        ApplicationSessionState actual)
    {
        Assert.Equal(expected.Party.PartyId, actual.Party.PartyId);
        Assert.Equal(expected.RandomSeed, actual.RandomSeed);
        Assert.Equal(
            expected.RandomValuesConsumed,
            actual.RandomValuesConsumed);
        Assert.Equal(
            expected.Party.Members.Select(member =>
                member.PartyMemberId),
            actual.Party.Members.Select(member =>
                member.PartyMemberId));

        for (int index = 0;
            index < expected.Party.Members.Count;
            index++)
        {
            PartyMemberState expectedMember =
                expected.Party.Members[index];
            PartyMemberState actualMember =
                actual.Party.Members[index];

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
                expectedMember.Health.HitPoints,
                actualMember.Health.HitPoints);
            Assert.Equal(
                expectedMember.Health.DeathSavingThrows,
                actualMember.Health.DeathSavingThrows);
            Assert.Equal(
                expectedMember.Health.IsInstantlyDead,
                actualMember.Health.IsInstantlyDead);
            Assert.Equal(
                expectedMember.Health.IsDead,
                actualMember.Health.IsDead);
            Assert.Equal(
                expectedMember.Ammunition,
                actualMember.Ammunition);
        }
    }
}
