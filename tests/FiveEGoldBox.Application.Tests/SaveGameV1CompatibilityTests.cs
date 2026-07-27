using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Tests;

public sealed class SaveGameV1CompatibilityTests
{
    private const string OutpostFixture =
        "v1-outpost-four-character-party.json";

    private const string ExplorationFixture =
        "v1-exploration-four-character-party.json";

    private const string ScenarioConclusionFixture =
        "v1-scenario-conclusion-four-character-party.json";

    /// Written when the campaign fielded Fighter, Barbarian and Ranger. Kept
    /// rather than regenerated: they are the only evidence a V1 document
    /// written by an older build still parses, and that is what they still
    /// prove — they now get as far as session validation and are turned away
    /// there, rather than failing to be read at all.
    private static readonly string[] ThreeCharacterPartyFixtures =
    [
        "v1-outpost-mission-not-accepted.json",
        "v1-exploration-upper-floor.json",
        "v1-scenario-conclusion-party-defeated.json"
    ];

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
            WatchtowerScenario.ProgressOf(loaded));
        Assert.Equal(424242, loaded.RandomSeed);
        Assert.Equal(4, loaded.Party.Members.Count);
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
            WatchtowerScenario.ProgressOf(loaded));

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
            WatchtowerScenario.ProgressOf(loaded));
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
        Assert.Equal(WatchtowerScenario.ProgressOf(second), WatchtowerScenario.ProgressOf(first));
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

    /// The roster changed, and a save naming a party this campaign no longer
    /// fields is well-formed data describing an invalid session — the same
    /// call PR #107 made for an unrecognised progress marker. That it fails
    /// here rather than while being read is the part worth pinning: the V1
    /// format itself is untouched.
    [Fact]
    public void Deserialize_AThreeCharacterPartySave_ParsesAndIsRejected()
    {
        Assert.All(
            ThreeCharacterPartyFixtures,
            fixtureFileName =>
            {
                ManualSaveLoadResult result =
                    LoadRawFixture(fixtureFileName);

                Assert.False(result.IsSuccess);
                Assert.Null(result.Session);
                Assert.Equal(
                    ManualSaveLoadFailureReason.InvalidSessionState,
                    result.FailureReason);
            });
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
