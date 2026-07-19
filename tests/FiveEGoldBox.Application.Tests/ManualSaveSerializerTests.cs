using System.Text.Json.Nodes;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class ManualSaveSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_ValidOutpostSession_PreservesMode()
    {
        ManualSaveLoadResult result = RoundTrip();

        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureReason);
        Assert.Equal(
            ApplicationMode.Outpost,
            AssertLoadedSession(result).CurrentMode);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesLocation()
    {
        ApplicationSessionState loaded =
            AssertLoadedSession(RoundTrip());

        Assert.Equal(
            "location.outpost",
            loaded.CurrentLocationId);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesPartyOrder()
    {
        ApplicationSessionState loaded =
            AssertLoadedSession(RoundTrip());

        Assert.Equal(
            new[]
            {
                "class.fighter",
                "class.barbarian",
                "class.ranger"
            },
            loaded.Party.Members.Select(
                member => member.ClassId));
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesStableMemberIds()
    {
        ApplicationSessionState loaded =
            AssertLoadedSession(RoundTrip());

        Assert.Equal(
            new[]
            {
                "party-member.fighter",
                "party-member.barbarian",
                "party-member.ranger"
            },
            loaded.Party.Members.Select(
                member => member.PartyMemberId));
        Assert.Equal(
            new[]
            {
                "character.fighter",
                "character.barbarian",
                "character.ranger"
            },
            loaded.Party.Members.Select(
                member => member.CharacterDefinitionId));
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesHealth()
    {
        ApplicationSessionState loaded =
            AssertLoadedSession(RoundTrip());

        CombatantHealthState barbarianHealth =
            loaded.Party.Members[1].Health;

        Assert.Equal(
            14,
            barbarianHealth.HitPoints.MaximumHitPoints);
        Assert.Equal(
            0,
            barbarianHealth.HitPoints.CurrentHitPoints);
        Assert.Equal(
            1,
            barbarianHealth.DeathSavingThrows.SuccessCount);
        Assert.Equal(
            1,
            barbarianHealth.DeathSavingThrows.FailureCount);
        Assert.False(
            barbarianHealth.DeathSavingThrows.IsStable);
        Assert.False(barbarianHealth.IsInstantlyDead);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesTemporaryHitPoints()
    {
        ApplicationSessionState loaded =
            AssertLoadedSession(RoundTrip());

        Assert.Equal(
            3,
            loaded.Party.Members[0]
                .Health.HitPoints.TemporaryHitPoints);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesRangerAmmunition()
    {
        ApplicationSessionState loaded =
            AssertLoadedSession(RoundTrip());

        AmmunitionState? ammunition =
            loaded.Party.Members[2].Ammunition;

        Assert.NotNull(ammunition);
        Assert.Equal(
            "weapon.longbow",
            ammunition.WeaponId);
        Assert.Equal(
            "item.arrow",
            ammunition.AmmunitionItemId);
        Assert.Equal(
            7,
            ammunition.RemainingQuantity);
    }

    [Fact]
    public void SerializeAndDeserialize_WithMissionNotAccepted_PreservesProgress()
    {
        ApplicationSessionState session =
            CreateMissionNotAcceptedSession();

        ApplicationSessionState loaded =
            AssertLoadedSession(
                ManualSaveSerializer.Deserialize(
                    ManualSaveSerializer.Serialize(
                        session)));

        Assert.Equal(
            WatchtowerScenarioProgress
                .MissionNotAccepted,
            loaded.Scenario.Progress);
        Assert.Equal(
            ApplicationMode.Outpost,
            loaded.CurrentMode);
    }

    [Fact]
    public void SerializeAndDeserialize_WithMissionAccepted_PreservesPersistentState()
    {
        ApplicationSessionState initial =
            CreateMissionNotAcceptedSession();
        ApplicationSessionState accepted =
            OutpostMissionRules.Resolve(
                initial,
                OutpostMissionChoice.AcceptMission)
                .State;

        ApplicationSessionState loaded =
            AssertLoadedSession(
                ManualSaveSerializer.Deserialize(
                    ManualSaveSerializer.Serialize(
                        accepted)));

        Assert.Equal(
            ApplicationMode.Outpost,
            loaded.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            loaded.Scenario.Progress);
        Assert.Equal(
            accepted.CurrentLocationId,
            loaded.CurrentLocationId);
        Assert.Equal(
            accepted.RandomSeed,
            loaded.RandomSeed);
        Assert.Equal(
            accepted.RandomValuesConsumed,
            loaded.RandomValuesConsumed);
        Assert.Equal(
            accepted.Party.PartyId,
            loaded.Party.PartyId);
        Assert.Equal(
            accepted.Party.Members.Select(
                member => member.PartyMemberId),
            loaded.Party.Members.Select(
                member => member.PartyMemberId));
        Assert.Equal(
            accepted.Party.Members.Select(
                member => member.ClassId),
            loaded.Party.Members.Select(
                member => member.ClassId));
        Assert.Equal(
            accepted.Party.Members.Select(
                member => member.ZeroHitPointPolicy),
            loaded.Party.Members.Select(
                member => member.ZeroHitPointPolicy));
        Assert.Equal(
            accepted.Party.Members.Select(
                member => member.Health),
            loaded.Party.Members.Select(
                member => member.Health));
        Assert.Equal(
            accepted.Party.Members.Select(
                member => member.Ammunition),
            loaded.Party.Members.Select(
                member => member.Ammunition));
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesScenarioProgress()
    {
        ApplicationSessionState loaded =
            AssertLoadedSession(RoundTrip());

        Assert.Equal(
            WatchtowerScenarioProgress.RaidersDefeated,
            loaded.Scenario.Progress);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesRandomState()
    {
        ApplicationSessionState loaded =
            AssertLoadedSession(RoundTrip());

        Assert.Equal(8675309, loaded.RandomSeed);
        Assert.Equal(12, loaded.RandomValuesConsumed);
    }

    [Fact]
    public void Serialize_UsesStringEnumNames()
    {
        string serialized = ManualSaveSerializer.Serialize(
            CreateRoundTripSession());

        Assert.Contains(
            "\"CurrentMode\":\"Outpost\"",
            serialized,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"Progress\":\"RaidersDefeated\"",
            serialized,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_DoesNotPersistDerivedHealthProperties()
    {
        string serialized = ManualSaveSerializer.Serialize(
            CreateRoundTripSession());

        Assert.DoesNotContain(
            "\"IsAtZeroHitPoints\"",
            serialized,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "\"IsDead\"",
            serialized,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_WithMalformedJson_ReturnsStructuredFailure()
    {
        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize("{not-json");

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .MalformedSerializedData);
    }

    [Fact]
    public void Deserialize_WithBlankData_ReturnsStructuredFailure()
    {
        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(" ");

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .MalformedSerializedData);
    }

    [Fact]
    public void Deserialize_WithMissingRequiredData_ReturnsStructuredFailure()
    {
        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                "{\"FormatVersion\":1}");

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .MalformedSerializedData);
    }

    [Fact]
    public void Deserialize_WithUnsupportedVersion_ReturnsStructuredFailure()
    {
        JsonObject save = ParseSave(
            ManualSaveSerializer.Serialize(
                CreateRoundTripSession()));
        save["FormatVersion"] = 999;

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                save.ToJsonString());

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .UnsupportedFormatVersion);
    }

    [Fact]
    public void Deserialize_WithUndefinedEnum_ReturnsStructuredFailure()
    {
        JsonObject save = ParseSave(
            ManualSaveSerializer.Serialize(
                CreateRoundTripSession()));
        JsonObject session = GetObject(
            save,
            "Session");
        JsonObject scenario = GetObject(
            session,
            "Scenario");
        scenario["Progress"] = "UnknownProgress";

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                save.ToJsonString());

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .MalformedSerializedData);
    }

    [Fact]
    public void Deserialize_WithNumericEnum_ReturnsStructuredFailure()
    {
        JsonObject save = ParseSave(
            ManualSaveSerializer.Serialize(
                CreateRoundTripSession()));
        JsonObject session = GetObject(
            save,
            "Session");
        session["CurrentMode"] = 0;

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                save.ToJsonString());

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .MalformedSerializedData);
    }

    [Fact]
    public void Deserialize_WithInvalidNestedState_ReturnsStructuredFailure()
    {
        JsonObject save = ParseSave(
            ManualSaveSerializer.Serialize(
                CreateRoundTripSession()));
        JsonObject session = GetObject(
            save,
            "Session");
        JsonObject party = GetObject(
            session,
            "Party");
        JsonArray members = GetArray(
            party,
            "Members");
        JsonObject fighter = Assert.IsType<JsonObject>(
            members[0]);
        JsonObject health = GetObject(
            fighter,
            "Health");
        JsonObject hitPoints = GetObject(
            health,
            "HitPoints");
        hitPoints["CurrentHitPoints"] = -1;

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                save.ToJsonString());

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .InvalidSessionState);
    }

    [Fact]
    public void Serialize_WithNonOutpostMode_Throws()
    {
        ApplicationSessionState state =
            CreateRoundTripSession() with
            {
                CurrentMode = ApplicationMode.Exploration
            };

        Assert.Throws<ArgumentException>(() =>
            ManualSaveSerializer.Serialize(state));
    }

    [Fact]
    public void Deserialize_WithNonOutpostMode_ReturnsInvalidStateFailure()
    {
        JsonObject save = ParseSave(
            ManualSaveSerializer.Serialize(
                CreateRoundTripSession()));
        JsonObject session = GetObject(
            save,
            "Session");
        session["CurrentMode"] = "Exploration";

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                save.ToJsonString());

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .InvalidSessionState);
    }

    [Fact]
    public void Deserialize_Version1OutpostSaveWithoutTravelProperty_RemainsValid()
    {
        JsonObject save = ParseSave(
            ManualSaveSerializer.Serialize(
                CreateRoundTripSession()));
        JsonObject session = GetObject(
            save,
            "Session");
        _ = session.Remove("RegionalTravel");

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                save.ToJsonString());

        Assert.True(result.IsSuccess);
        ApplicationSessionState loaded =
            AssertLoadedSession(result);
        Assert.Equal(
            ApplicationMode.Outpost,
            loaded.CurrentMode);
        Assert.Null(loaded.RegionalTravel);
    }

    [Fact]
    public void Serialize_WithRegionalTravelMode_Throws()
    {
        ApplicationSessionState traveling =
            CreateRegionalTravelSession();

        Assert.Throws<ArgumentException>(() =>
            ManualSaveSerializer.Serialize(traveling));
    }

    [Fact]
    public void Deserialize_WithRegionalTravelModeAndMissingTravel_ReturnsInvalidStateFailure()
    {
        JsonObject save = ParseSave(
            ManualSaveSerializer.Serialize(
                CreateMissionNotAcceptedSession()));
        JsonObject session = GetObject(
            save,
            "Session");
        session["CurrentMode"] = "RegionalTravel";
        _ = session.Remove("RegionalTravel");

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                save.ToJsonString());

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .InvalidSessionState);
    }

    [Fact]
    public void Deserialize_WithOutpostModeAndTravelState_ReturnsInvalidStateFailure()
    {
        ApplicationSessionState traveling =
            CreateRegionalTravelSession();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                traveling.RegionalTravel);
        JsonObject save = ParseSave(
            ManualSaveSerializer.Serialize(
                CreateMissionNotAcceptedSession()));
        JsonObject session = GetObject(
            save,
            "Session");
        session["RegionalTravel"] =
            CreateTravelJson(travel);

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                save.ToJsonString());

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .InvalidSessionState);
    }

    [Fact]
    public void Deserialize_WithValidRegionalTravelState_ReturnsInvalidStateFailure()
    {
        ApplicationSessionState traveling =
            CreateRegionalTravelSession();
        RegionalTravelState travel =
            Assert.IsType<RegionalTravelState>(
                traveling.RegionalTravel);
        JsonObject save = ParseSave(
            ManualSaveSerializer.Serialize(
                CreateMissionNotAcceptedSession()));
        JsonObject session = GetObject(
            save,
            "Session");
        session["CurrentMode"] = "RegionalTravel";
        GetObject(session, "Scenario")["Progress"] =
            "MissionAccepted";
        session["RegionalTravel"] =
            CreateTravelJson(travel);

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(
                save.ToJsonString());

        AssertFailure(
            result,
            ManualSaveLoadFailureReason
                .InvalidSessionState);
    }

    [Fact]
    public void Deserialize_ProtectsLoadedMembersWithReadOnlyCollection()
    {
        string serialized = ManualSaveSerializer.Serialize(
            CreateRoundTripSession());
        ApplicationSessionState loaded =
            AssertLoadedSession(
                ManualSaveSerializer.Deserialize(serialized));

        Assert.IsType<
            System.Collections.ObjectModel
                .ReadOnlyCollection<PartyMemberState>>(
                    loaded.Party.Members);
    }

    private static JsonObject CreateTravelJson(
        RegionalTravelState travel)
    {
        return new JsonObject
        {
            ["RouteId"] = travel.RouteId,
            ["OriginLocationId"] =
                travel.OriginLocationId,
            ["DestinationLocationId"] =
                travel.DestinationLocationId,
            ["CurrentStepIndex"] =
                travel.CurrentStepIndex,
            ["FinalStepIndex"] =
                travel.FinalStepIndex
        };
    }

    private static ApplicationSessionState
        CreateRegionalTravelSession()
    {
        ApplicationSessionState accepted =
            OutpostMissionRules.Resolve(
                CreateMissionNotAcceptedSession(),
                OutpostMissionChoice.AcceptMission)
                .State;

        return RegionalTravelRules
            .BeginWatchtowerJourney(accepted);
    }

    private static ManualSaveLoadResult RoundTrip()
    {
        string serialized = ManualSaveSerializer.Serialize(
            CreateRoundTripSession());

        return ManualSaveSerializer.Deserialize(serialized);
    }

    private static ApplicationSessionState
        AssertLoadedSession(
            ManualSaveLoadResult result)
    {
        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureReason);

        return Assert.IsType<ApplicationSessionState>(
            result.Session);
    }

    private static void AssertFailure(
        ManualSaveLoadResult result,
        ManualSaveLoadFailureReason expectedReason)
    {
        Assert.False(result.IsSuccess);
        Assert.Null(result.Session);
        Assert.Equal(
            expectedReason,
            result.FailureReason);
    }

    private static JsonObject ParseSave(
        string serialized)
    {
        return Assert.IsType<JsonObject>(
            JsonNode.Parse(serialized));
    }

    private static JsonObject GetObject(
        JsonObject parent,
        string propertyName)
    {
        return Assert.IsType<JsonObject>(
            parent[propertyName]);
    }

    private static JsonArray GetArray(
        JsonObject parent,
        string propertyName)
    {
        return Assert.IsType<JsonArray>(
            parent[propertyName]);
    }

    private static ApplicationSessionState
        CreateMissionNotAcceptedSession()
    {
        return CreateRoundTripSession() with
        {
            Scenario = new WatchtowerScenarioState
            {
                Progress =
                    WatchtowerScenarioProgress
                        .MissionNotAccepted
            }
        };
    }

    private static ApplicationSessionState
        CreateRoundTripSession()
    {
        PartyMemberState[] members =
            CreateValidMembers();

        members[0] = members[0] with
        {
            Health = CombatantHealthRules.Create(
                maximumHitPoints: 12) with
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints = 12,
                    CurrentHitPoints = 5,
                    TemporaryHitPoints = 3
                }
            }
        };

        members[1] = members[1] with
        {
            Health = CombatantHealthRules.Create(
                maximumHitPoints: 14) with
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints = 14,
                    CurrentHitPoints = 0,
                    TemporaryHitPoints = 0
                },
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 1,
                        FailureCount = 1
                    }
            }
        };

        members[2] = members[2] with
        {
            Ammunition = new AmmunitionState
            {
                WeaponId = "weapon.longbow",
                AmmunitionItemId = "item.arrow",
                RemainingQuantity = 7
            }
        };

        ApplicationSessionState state =
            ApplicationSessionRules.CreateNew(
                scenarioId: "scenario.watchtower",
                currentLocationId: "location.outpost",
                party: new PartyState
                {
                    PartyId = "party.player",
                    Members = members
                },
                randomSeed: 8675309);

        return state with
        {
            Scenario = new WatchtowerScenarioState
            {
                Progress =
                    WatchtowerScenarioProgress
                        .RaidersDefeated
            },
            RandomValuesConsumed = 12
        };
    }

    private static PartyMemberState[]
        CreateValidMembers()
    {
        return
        [
            CreateMember(
                partyMemberId:
                    "party-member.fighter",
                characterDefinitionId:
                    "character.fighter",
                displayName: "Fighter",
                classId: "class.fighter",
                maximumHitPoints: 12),
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
                    RemainingQuantity = 18
                }
            }
        ];
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
