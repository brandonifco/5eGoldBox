using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Tests;

public sealed class SaveGameV1CompatibilityTests
{
    private const string OutpostFixture =
        "v1-outpost-mission-not-accepted.json";

    private const string ExplorationFixture =
        "v1-exploration-upper-floor.json";

    private const string ScenarioConclusionFixture =
        "v1-scenario-conclusion-party-defeated.json";

    private const string MalformedFixture =
        "v1-malformed-negative-hit-points.json";

    [Fact]
    public void Deserialize_OutpostFixture_Loads()
    {
        ApplicationSessionState loaded =
            LoadFixture(OutpostFixture);

        Assert.Equal(
            ApplicationMode.Outpost,
            loaded.CurrentMode);
        Assert.Equal(
            "location.outpost",
            loaded.CurrentLocationId);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionNotAccepted,
            loaded.Scenario.Progress);
        Assert.Equal(424242, loaded.RandomSeed);
        Assert.Equal(3, loaded.Party.Members.Count);
        Assert.Null(loaded.Exploration);
        Assert.Null(loaded.RegionalTravel);
        Assert.Null(loaded.ActiveEncounter);
    }

    [Fact]
    public void Deserialize_ExplorationFixture_Loads()
    {
        ApplicationSessionState loaded =
            LoadFixture(ExplorationFixture);

        Assert.Equal(
            ApplicationMode.Exploration,
            loaded.CurrentMode);
        Assert.Equal(
            "location.ruined-watchtower",
            loaded.CurrentLocationId);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            loaded.Scenario.Progress);

        ExplorationState exploration =
            Assert.IsType<ExplorationState>(
                loaded.Exploration);

        Assert.Equal(
            "map.ruined-watchtower",
            exploration.MapId);
        Assert.Equal(
            ExplorationFloor.UpperFloor,
            exploration.Floor);
        Assert.Equal(
            ExplorationFacing.East,
            exploration.Facing);
    }

    [Fact]
    public void Deserialize_ScenarioConclusionFixture_Loads()
    {
        ApplicationSessionState loaded =
            LoadFixture(ScenarioConclusionFixture);

        Assert.Equal(
            ApplicationMode.ScenarioConclusion,
            loaded.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.PartyDefeated,
            loaded.Scenario.Progress);
        Assert.All(
            loaded.Party.Members,
            member => Assert.Equal(
                0,
                member.Health.HitPoints.CurrentHitPoints));
    }

    [Theory]
    [InlineData(OutpostFixture)]
    [InlineData(ExplorationFixture)]
    [InlineData(ScenarioConclusionFixture)]
    public void LoadSaveLoad_PreservesSemanticState(
        string fixtureFileName)
    {
        ApplicationSessionState first =
            LoadFixture(fixtureFileName);

        string reserialized =
            ManualSaveSerializer.Serialize(first);
        ApplicationSessionState second =
            LoadFixtureFromJson(reserialized);

        // ApplicationSessionState.Party.Members is IReadOnlyList<T>-typed,
        // so record-generated Equals compares it by reference, not value.
        // Compare the fields that actually matter instead of the whole graph.
        Assert.Equal(second.ScenarioId, first.ScenarioId);
        Assert.Equal(second.CurrentMode, first.CurrentMode);
        Assert.Equal(second.CurrentLocationId, first.CurrentLocationId);
        Assert.Equal(second.Scenario.Progress, first.Scenario.Progress);
        Assert.Equal(second.RandomSeed, first.RandomSeed);
        Assert.Equal(
            second.RandomValuesConsumed,
            first.RandomValuesConsumed);
        Assert.Equal(second.Party.PartyId, first.Party.PartyId);
        Assert.Equal(
            first.Party.Members.Select(member => member.PartyMemberId),
            second.Party.Members.Select(member => member.PartyMemberId));
        Assert.Equal(
            first.Party.Members.Select(member => member.Health),
            second.Party.Members.Select(member => member.Health));
        Assert.Equal(
            first.Party.Members.Select(member => member.Ammunition),
            second.Party.Members.Select(member => member.Ammunition));
        Assert.Equal(first.RegionalTravel, second.RegionalTravel);
        Assert.Equal(first.Exploration, second.Exploration);
        Assert.Equal(first.ActiveEncounter, second.ActiveEncounter);
    }

    [Fact]
    public void Deserialize_MalformedFixture_FailsDeterministically()
    {
        ManualSaveLoadResult result =
            LoadRawFixture(MalformedFixture);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Session);
        Assert.Equal(
            ManualSaveLoadFailureReason.InvalidSessionState,
            result.FailureReason);
    }

    private static ApplicationSessionState LoadFixture(
        string fixtureFileName)
    {
        ManualSaveLoadResult result =
            LoadRawFixture(fixtureFileName);

        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureReason);

        return Assert.IsType<ApplicationSessionState>(
            result.Session);
    }

    private static ManualSaveLoadResult LoadRawFixture(
        string fixtureFileName)
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            fixtureFileName);
        string json = File.ReadAllText(path);

        return ManualSaveSerializer.Deserialize(json);
    }

    private static ApplicationSessionState LoadFixtureFromJson(
        string json)
    {
        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(json);

        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureReason);

        return Assert.IsType<ApplicationSessionState>(
            result.Session);
    }
}
