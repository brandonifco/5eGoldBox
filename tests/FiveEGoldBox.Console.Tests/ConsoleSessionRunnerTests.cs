using System.Text.Json.Nodes;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Console.Tests;

public sealed class ConsoleSessionRunnerTests
{
    private const int RandomSeed = 24680;

    [Fact]
    public void Run_WithNullInput_Throws()
    {
        ConsoleSessionRunner runner = new();

        Assert.Throws<ArgumentNullException>(() =>
            runner.Run(
                null!,
                new StringWriter(),
                "savegame.json",
                RandomSeed));
    }

    [Fact]
    public void Run_WithNullOutput_Throws()
    {
        ConsoleSessionRunner runner = new();

        Assert.Throws<ArgumentNullException>(() =>
            runner.Run(
                new StringReader(string.Empty),
                null!,
                "savegame.json",
                RandomSeed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Run_WithInvalidSavePath_Throws(
        string? savePath)
    {
        ConsoleSessionRunner runner = new();

        Assert.Throws<ArgumentException>(() =>
            runner.Run(
                new StringReader(string.Empty),
                new StringWriter(),
                savePath!,
                RandomSeed));
    }

    [Fact]
    public void Run_WithEofAtStartMenu_ReturnsZero()
    {
        using TemporaryDirectory temporary = new();
        ConsoleSessionRunner runner = new();

        int exitCode = runner.Run(
            new StringReader(string.Empty),
            new StringWriter(),
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Run_WithEofInSessionMenu_ReturnsZero()
    {
        using TemporaryDirectory temporary = new();
        ConsoleSessionRunner runner = new();

        int exitCode = runner.Run(
            new StringReader("1\n"),
            new StringWriter(),
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Run_WithExplicitStartMenuExit_ReturnsZero()
    {
        using TemporaryDirectory temporary = new();
        ConsoleSessionRunner runner = new();

        int exitCode = runner.Run(
            new StringReader("3\n"),
            new StringWriter(),
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Run_WithExplicitSessionExit_ReturnsZero()
    {
        using TemporaryDirectory temporary = new();
        ConsoleSessionRunner runner = new();

        int exitCode = runner.Run(
            new StringReader("1\n5\n"),
            new StringWriter(),
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Run_ExitDoesNotCreateImplicitSave()
    {
        using TemporaryDirectory temporary = new();
        ConsoleSessionRunner runner = new();

        _ = runner.Run(
            new StringReader("1\n5\n"),
            new StringWriter(),
            temporary.SavePath,
            RandomSeed);

        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void Run_NewGameDisplaysCanonicalOutpostState()
    {
        using TemporaryDirectory temporary = new();

        string output = Run(
            "1\n5\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Contains("Mode: Outpost", output);
        Assert.Contains("Scenario: scenario.watchtower", output);
        Assert.Contains("Location: location.outpost", output);
        Assert.Contains("Progress: MissionNotAccepted", output);
        Assert.Contains(
            "Outpost State: Party is at the current outpost.",
            output);
    }

    [Fact]
    public void Run_NewGameDisplaysConfiguredSeedUnchanged()
    {
        using TemporaryDirectory temporary = new();

        string output = Run(
            "1\n5\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Contains($"Random Seed: {RandomSeed}", output);
    }

    [Fact]
    public void Run_RepeatedEquivalentNewGamesProduceEquivalentOutput()
    {
        using TemporaryDirectory firstTemporary = new();
        using TemporaryDirectory secondTemporary = new();

        string firstOutput = Run(
            "1\n5\n",
            firstTemporary.SavePath,
            RandomSeed);
        string secondOutput = Run(
            "1\n5\n",
            secondTemporary.SavePath,
            RandomSeed);

        Assert.Equal(firstOutput, secondOutput);
    }

    [Fact]
    public void Run_InvalidStartSelectionRepeatsWithoutCreatingSession()
    {
        using TemporaryDirectory temporary = new();

        string output = Run(
            "9\n3\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(
            2,
            CountOccurrences(
                output,
                "5eGoldBox Console Reference Client"));
        Assert.Contains("Invalid selection.", output);
        Assert.DoesNotContain("Session Summary", output);
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void Run_LoadValidOutpostSaveRoutesCorrectly()
    {
        using TemporaryDirectory temporary = new();
        WriteSave(
            temporary.SavePath,
            CreateOutpostSession());

        string output = Run(
            "2\n5\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Contains("Mode: Outpost", output);
        Assert.Contains("Manual Save Available: Yes", output);
    }

    [Fact]
    public void Run_LoadValidExplorationSaveRoutesCorrectly()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateExplorationSession();
        WriteSave(temporary.SavePath, session);

        string output = Run(
            "2\n6\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Contains("Mode: Exploration", output);
        Assert.Contains(
            $"Map ID: {session.Exploration!.MapId}",
            output);
    }

    [Fact]
    public void Run_LoadValidScenarioConclusionSaveRoutesCorrectly()
    {
        using TemporaryDirectory temporary = new();
        WriteSave(
            temporary.SavePath,
            CreateScenarioConclusionSession());

        string output = Run(
            "2\n3\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Contains("Mode: ScenarioConclusion", output);
        Assert.Contains("Scenario Conclusion", output);
        Assert.Contains("Conclusion Progress: PartyDefeated", output);
    }

    [Fact]
    public void Run_LoadMissingFileReportsRecoverably()
    {
        using TemporaryDirectory temporary = new();

        string output = Run(
            "2\n3\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Contains("Load failed: save file not found", output);
        Assert.Equal(
            2,
            CountOccurrences(
                output,
                "5eGoldBox Console Reference Client"));
    }

    [Fact]
    public void Run_LoadUnreadablePathReportsRecoverably()
    {
        using TemporaryDirectory temporary = new();

        string output = Run(
            "2\n3\n",
            temporary.DirectoryPath,
            RandomSeed);

        Assert.Contains("Load failed:", output);
        Assert.Equal(
            2,
            CountOccurrences(
                output,
                "5eGoldBox Console Reference Client"));
    }

    [Fact]
    public void Run_LoadMalformedSaveReportsStructuredReason()
    {
        using TemporaryDirectory temporary = new();
        File.WriteAllText(
            temporary.SavePath,
            "not valid json");

        string output = Run(
            "2\n3\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Contains("malformed save data", output);
    }

    [Fact]
    public void Run_LoadUnsupportedFormatReportsStructuredReason()
    {
        using TemporaryDirectory temporary = new();
        JsonObject root = CreateSaveJson(
            CreateOutpostSession());
        root["FormatVersion"] = 999;
        File.WriteAllText(
            temporary.SavePath,
            root.ToJsonString());

        string output = Run(
            "2\n3\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Contains(
            "unsupported save format version",
            output);
    }

    [Fact]
    public void Run_LoadInvalidSessionReportsStructuredReason()
    {
        using TemporaryDirectory temporary = new();
        JsonObject root = CreateSaveJson(
            CreateOutpostSession());
        JsonObject session =
            root["Session"]!.AsObject();
        session["CurrentLocationId"] = string.Empty;
        File.WriteAllText(
            temporary.SavePath,
            root.ToJsonString());

        string output = Run(
            "2\n3\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Contains("invalid saved session state", output);
    }

    [Fact]
    public void Run_FailedLoadReturnsToStartMenu()
    {
        using TemporaryDirectory temporary = new();
        File.WriteAllText(
            temporary.SavePath,
            "not valid json");

        string output = Run(
            "2\n3\n",
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(
            2,
            CountOccurrences(
                output,
                "5eGoldBox Console Reference Client"));
        Assert.DoesNotContain("Session Menu", output);
    }

    [Fact]
    public void RunSession_SaveIsOfferedForValidOutpostSession()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateOutpostSession());

        Assert.Contains("4. Save", output);
        Assert.Contains("5. Exit", output);
    }

    [Fact]
    public void RunSession_SaveIsOfferedForValidExplorationSession()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateExplorationSession());

        Assert.Contains("5. Save", output);
        Assert.Contains("6. Exit", output);
    }

    [Fact]
    public void RunSession_SaveIsOfferedForValidScenarioConclusionSession()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateScenarioConclusionSession());

        Assert.Contains("2. Save", output);
        Assert.Contains("3. Exit", output);
    }

    [Fact]
    public void RunSession_SaveIsNotOfferedInRegionalTravel()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateRegionalTravelSession());

        Assert.DoesNotContain("Save", GetSessionMenu(output));
        Assert.Contains("1. Advance Travel", output);
        Assert.Contains("2. Inspect Party", output);
        Assert.Contains("3. Exit", output);
    }

    [Fact]
    public void RunSession_SaveIsNotOfferedInEncounter()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateEncounterSession());

        Assert.DoesNotContain("2. Save", output);
        Assert.Contains("2. Exit", output);
    }

    [Fact]
    public void RunSession_SaveWritesExactlySerializerOutput()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateOutpostSession();

        _ = RunSession(
            "4\n5\n",
            temporary.SavePath,
            session);

        Assert.Equal(
            ManualSaveSerializer.Serialize(session),
            File.ReadAllText(temporary.SavePath));
    }

    [Fact]
    public void RunSession_SaveOverwritesConfiguredFileDeterministically()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateExplorationSession();
        File.WriteAllText(temporary.SavePath, "old data");

        _ = RunSession(
            "5\n6\n",
            temporary.SavePath,
            session);

        Assert.Equal(
            ManualSaveSerializer.Serialize(session),
            File.ReadAllText(temporary.SavePath));
    }

    [Fact]
    public void RunSession_SaveWriteFailureIsRecoverable()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateOutpostSession();

        string output = RunSession(
            "4\n5\n",
            temporary.DirectoryPath,
            session);

        Assert.Contains("Save failed:", output);
        Assert.Equal(
            2,
            CountOccurrences(output, "Session Menu"));
        Assert.Equal(ApplicationMode.Outpost, session.CurrentMode);
    }

    [Fact]
    public void RunSession_SuccessfulSaveDoesNotChangeSession()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateExplorationSession();
        var party = session.Party;
        var scenario = session.Scenario;
        var exploration = session.Exploration;
        int randomValuesConsumed =
            session.RandomValuesConsumed;

        _ = RunSession(
            "5\n6\n",
            temporary.SavePath,
            session);

        Assert.Equal(ApplicationMode.Exploration, session.CurrentMode);
        Assert.Same(party, session.Party);
        Assert.Same(scenario, session.Scenario);
        Assert.Same(exploration, session.Exploration);
        Assert.Equal(
            randomValuesConsumed,
            session.RandomValuesConsumed);
    }

    [Fact]
    public void Run_FileWrittenByOneInvocationLoadsInFreshInvocation()
    {
        using TemporaryDirectory temporary = new();
        ConsoleSessionRunner firstRunner = new();
        ConsoleSessionRunner secondRunner = new();

        _ = firstRunner.RunSession(
            new StringReader("5\n6\n"),
            new StringWriter(),
            temporary.SavePath,
            CreateExplorationSession());
        StringWriter secondOutput = new();

        int exitCode = secondRunner.Run(
            new StringReader("2\n6\n"),
            secondOutput,
            temporary.SavePath,
            RandomSeed);

        Assert.Equal(0, exitCode);
        Assert.Contains(
            "Mode: Exploration",
            secondOutput.ToString());
    }

    [Fact]
    public void RenderSessionSummary_OutpostDisplaysRequiredCommonFields()
    {
        ApplicationSessionState session =
            CreateOutpostSession();

        string output = RenderSummary(session);

        Assert.Contains($"Mode: {session.CurrentMode}", output);
        Assert.Contains($"Scenario: {session.ScenarioId}", output);
        Assert.Contains(
            $"Location: {session.CurrentLocationId}",
            output);
        Assert.Contains(
            $"Progress: {session.Scenario.Progress}",
            output);
        Assert.Contains(
            $"Random Seed: {session.RandomSeed}",
            output);
        Assert.Contains(
            $"Random Values Consumed: {session.RandomValuesConsumed}",
            output);
        Assert.Contains("Manual Save Available: Yes", output);
    }

    [Fact]
    public void RenderSessionSummary_RegionalTravelDisplaysRouteAndSteps()
    {
        ApplicationSessionState session =
            CreateRegionalTravelSession();
        var travel = session.RegionalTravel!;

        string output = RenderSummary(session);

        Assert.Contains($"Route ID: {travel.RouteId}", output);
        Assert.Contains(
            $"Origin Location ID: {travel.OriginLocationId}",
            output);
        Assert.Contains(
            $"Destination Location ID: {travel.DestinationLocationId}",
            output);
        Assert.Contains(
            $"Current Step Index: {travel.CurrentStepIndex}",
            output);
        Assert.Contains(
            $"Final Step Index: {travel.FinalStepIndex}",
            output);
        Assert.Contains("Travel Complete: No", output);
    }

    [Fact]
    public void RenderSessionSummary_ExplorationDisplaysMapFloorPositionAndFacing()
    {
        ApplicationSessionState session =
            CreateExplorationSession();
        var exploration = session.Exploration!;

        string output = RenderSummary(session);

        Assert.Contains($"Map ID: {exploration.MapId}", output);
        Assert.Contains($"Floor: {exploration.Floor}", output);
        Assert.Contains(
            $"Position X: {exploration.Position.X}",
            output);
        Assert.Contains(
            $"Position Y: {exploration.Position.Y}",
            output);
        Assert.Contains($"Facing: {exploration.Facing}", output);
    }

    [Fact]
    public void RenderSessionSummary_EncounterDisplaysIdentityLifecycleRevisionAndActor()
    {
        ApplicationSessionState session =
            CreateEncounterSession();
        var encounter = session.ActiveEncounter!.Encounter;

        string output = RenderSummary(session);

        Assert.Contains(
            $"Encounter ID: {encounter.EncounterId}",
            output);
        Assert.Contains(
            $"Encounter Lifecycle: {encounter.LifecycleState}",
            output);
        Assert.Contains(
            $"Encounter Revision: {encounter.Revision}",
            output);
        Assert.Contains(
            $"Active Combatant ID: {encounter.ActiveCombatantId}",
            output);
    }

    [Fact]
    public void RenderSessionSummary_ScenarioConclusionDisplaysTerminalProgress()
    {
        ApplicationSessionState session =
            CreateScenarioConclusionSession();

        string output = RenderSummary(session);

        Assert.Contains("Scenario Conclusion", output);
        Assert.Contains("Conclusion Progress: PartyDefeated", output);
        Assert.Contains(
            $"Conclusion Location: {session.CurrentLocationId}",
            output);
    }

    [Fact]
    public void RenderSessionSummary_DoesNotAdvanceGameplayOrRandomness()
    {
        ApplicationSessionState session =
            CreateEncounterSession();
        var activeEncounter = session.ActiveEncounter;
        long revision =
            session.ActiveEncounter!.Encounter.Revision;
        int randomValuesConsumed =
            session.RandomValuesConsumed;

        _ = RenderSummary(session);

        Assert.Same(activeEncounter, session.ActiveEncounter);
        Assert.Equal(
            revision,
            session.ActiveEncounter!.Encounter.Revision);
        Assert.Equal(
            randomValuesConsumed,
            session.RandomValuesConsumed);
    }

    [Fact]
    public void RenderSessionSummary_MalformedSessionFailureIsNotSwallowed()
    {
        ApplicationSessionState malformed =
            CreateOutpostSession() with
            {
                CurrentLocationId = string.Empty
            };
        ConsoleSessionRunner runner = new();

        Assert.Throws<ArgumentException>(() =>
            runner.RenderSessionSummary(
                new StringWriter(),
                malformed));
    }

    [Fact]
    public void RenderParty_PreservesAuthoritativeMemberOrder()
    {
        ApplicationSessionState session =
            CreateOutpostSession();

        string output = RenderParty(session);

        int fighterIndex = output.IndexOf(
            "Name: Fighter",
            StringComparison.Ordinal);
        int barbarianIndex = output.IndexOf(
            "Name: Barbarian",
            StringComparison.Ordinal);
        int rangerIndex = output.IndexOf(
            "Name: Ranger",
            StringComparison.Ordinal);

        Assert.True(fighterIndex >= 0);
        Assert.True(barbarianIndex > fighterIndex);
        Assert.True(rangerIndex > barbarianIndex);
    }

    [Fact]
    public void RenderParty_DisplaysIdentityHealthAndDeathState()
    {
        string output = RenderParty(
            CreateOutpostSession());

        Assert.Contains("Party ID: party.player", output);
        Assert.Contains("Party Member ID: party-member.fighter", output);
        Assert.Contains("Class ID: class.fighter", output);
        Assert.Contains("Hit Points: 8 / 12", output);
        Assert.Contains("Temporary Hit Points: 2", output);
        Assert.Contains("Stable: No", output);
        Assert.Contains("Dead: No", output);
        Assert.Contains("Instant Death: No", output);
        Assert.Contains("Death Save Successes: 0", output);
        Assert.Contains("Death Save Failures: 0", output);
    }

    [Fact]
    public void RenderParty_DisplaysRangerAmmunition()
    {
        string output = RenderParty(
            CreateOutpostSession());

        Assert.Contains("Weapon ID: weapon.longbow", output);
        Assert.Contains("Ammunition Item ID: item.arrow", output);
        Assert.Contains("Ammunition Remaining: 7", output);
    }

    [Fact]
    public void RenderParty_DoesNotMutateSessionOrRandomCursor()
    {
        ApplicationSessionState session =
            CreateOutpostSession();
        var party = session.Party;
        var members = session.Party.Members;
        int randomValuesConsumed =
            session.RandomValuesConsumed;

        _ = RenderParty(session);

        Assert.Same(party, session.Party);
        Assert.Same(members, session.Party.Members);
        Assert.Equal(
            randomValuesConsumed,
            session.RandomValuesConsumed);
    }

    [Fact]
    public void RunSession_InvalidSelectionRepeatsWithoutMutation()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateOutpostSession();
        var party = session.Party;

        string output = RunSession(
            "9\n5\n",
            temporary.SavePath,
            session);

        Assert.Contains("Invalid selection.", output);
        Assert.Equal(
            2,
            CountOccurrences(output, "Session Menu"));
        Assert.Same(party, session.Party);
        Assert.False(File.Exists(temporary.SavePath));
    }

    [Fact]
    public void RunSession_SaveableMenuNumberingIsStable()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateOutpostSession());

        Assert.Contains("1. Accept Mission", output);
        Assert.Contains("2. Not Yet", output);
        Assert.Contains("3. Inspect Party", output);
        Assert.Contains("4. Save", output);
        Assert.Contains("5. Exit", output);
    }

    [Fact]
    public void RunSession_UnsaveableMenuNumberingIsStable()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateRegionalTravelSession());

        Assert.Contains("1. Advance Travel", output);
        Assert.Contains("2. Inspect Party", output);
        Assert.Contains("3. Exit", output);
        Assert.DoesNotContain("4. Exit", output);
    }

    [Fact]
    public void RunSession_UnavailableSaveIsOmittedRatherThanDisabled()
    {
        using TemporaryDirectory temporary = new();

        string output = RunSession(
            string.Empty,
            temporary.SavePath,
            CreateEncounterSession());

        Assert.DoesNotContain("Save", GetSessionMenu(output));
        Assert.DoesNotContain("disabled", output);
        Assert.DoesNotContain("unavailable", output);
    }

    private static string Run(
        string input,
        string savePath,
        int randomSeed)
    {
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        int exitCode = runner.Run(
            new StringReader(input),
            output,
            savePath,
            randomSeed);

        Assert.Equal(0, exitCode);
        return output.ToString();
    }

    private static string RunSession(
        string input,
        string savePath,
        ApplicationSessionState session)
    {
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        int exitCode = runner.RunSession(
            new StringReader(input),
            output,
            savePath,
            session);

        Assert.Equal(0, exitCode);
        return output.ToString();
    }

    private static string RenderSummary(
        ApplicationSessionState session)
    {
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        runner.RenderSessionSummary(output, session);

        return output.ToString();
    }

    private static string RenderParty(
        ApplicationSessionState session)
    {
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        runner.RenderParty(output, session);

        return output.ToString();
    }

    private static ApplicationSessionState
        CreateOutpostSession()
    {
        return WatchtowerScenarioSessionFactory.CreateNew(
            RandomSeed);
    }

    private static ApplicationSessionState
        CreateRegionalTravelSession()
    {
        ApplicationSessionState accepted =
            OutpostMissionRules.Resolve(
                CreateOutpostSession(),
                OutpostMissionChoice.AcceptMission)
            .State;

        return RegionalTravelRules.BeginWatchtowerJourney(
            accepted);
    }

    private static ApplicationSessionState
        CreateExplorationSession()
    {
        ApplicationSessionState current =
            CreateRegionalTravelSession();

        while (RegionalTravelRules.CanAdvance(current))
        {
            current = RegionalTravelRules.Advance(current)
                .State;
        }

        return ExplorationRules.EnterWatchtower(current);
    }

    private static ApplicationSessionState
        CreateEncounterSession()
    {
        ApplicationSessionState current =
            CreateExplorationSession();

        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.UseStairs(current);
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);

        return SignalMechanismRules.Activate(current);
    }

    private static ApplicationSessionState
        CreateScenarioConclusionSession()
    {
        ApplicationSessionState exploration =
            CreateExplorationSession();
        ApplicationSessionState conclusion =
            exploration with
            {
                CurrentMode =
                    ApplicationMode.ScenarioConclusion,
                Scenario = exploration.Scenario with
                {
                    Progress =
                        WatchtowerScenarioProgress
                            .PartyDefeated
                },
                Exploration = null
            };

        ApplicationSessionRules.Validate(conclusion);

        return conclusion;
    }

    private static void WriteSave(
        string savePath,
        ApplicationSessionState session)
    {
        File.WriteAllText(
            savePath,
            ManualSaveSerializer.Serialize(session));
    }

    private static JsonObject CreateSaveJson(
        ApplicationSessionState session)
    {
        return JsonNode.Parse(
            ManualSaveSerializer.Serialize(session))!
        .AsObject();
    }

    private static int CountOccurrences(
        string value,
        string search)
    {
        int count = 0;
        int index = 0;

        while ((index = value.IndexOf(
            search,
            index,
            StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }

    private static string GetSessionMenu(string output)
    {
        int start = output.IndexOf(
            "Session Menu",
            StringComparison.Ordinal);

        Assert.True(start >= 0);

        return output[start..];
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"FiveEGoldBox.Console.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            SavePath = Path.Combine(
                DirectoryPath,
                "savegame.json");
        }

        internal string DirectoryPath { get; }

        internal string SavePath { get; }

        public void Dispose()
        {
            Directory.Delete(
                DirectoryPath,
                recursive: true);
        }
    }
}
