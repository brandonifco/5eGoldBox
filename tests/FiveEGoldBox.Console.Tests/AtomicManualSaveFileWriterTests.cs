using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Console.Tests;

public sealed class AtomicManualSaveFileWriterTests
{
    private const int RandomSeed = 24680;

    [Fact]
    public void SaveSession_WithNewDestination_WritesExactSerialization()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            WatchtowerScenarioSessionFactory.CreateNew(RandomSeed);
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        runner.SaveSession(
            output,
            temporary.SavePath,
            session);

        Assert.Equal(
            ManualSaveSerializer.Serialize(session),
            File.ReadAllText(temporary.SavePath));
        Assert.Contains(
            $"Game saved to {temporary.SavePath}.",
            output.ToString());
        Assert.Equal(
            [temporary.SavePath],
            Directory.GetFiles(temporary.DirectoryPath));
    }

    [Fact]
    public void SaveSession_WithExistingDestination_ReplacesExactContents()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            WatchtowerScenarioSessionFactory.CreateNew(RandomSeed);
        File.WriteAllText(
            temporary.SavePath,
            "old data");
        ConsoleSessionRunner runner = new();
        StringWriter output = new();

        runner.SaveSession(
            output,
            temporary.SavePath,
            session);

        string savedContents =
            File.ReadAllText(temporary.SavePath);

        Assert.Equal(
            ManualSaveSerializer.Serialize(session),
            savedContents);
        Assert.DoesNotContain(
            "old data",
            savedContents);
        Assert.Contains(
            $"Game saved to {temporary.SavePath}.",
            output.ToString());
        Assert.Equal(
            [temporary.SavePath],
            Directory.GetFiles(temporary.DirectoryPath));
    }

    [Fact]
    public void SaveSession_WhenTemporaryWriteFails_PreservesDestination()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            WatchtowerScenarioSessionFactory.CreateNew(RandomSeed);
        byte[] originalContents = "existing valid save"u8.ToArray();
        File.WriteAllBytes(
            temporary.SavePath,
            originalContents);
        string? temporaryPath = null;
        AtomicManualSaveFileWriter fileWriter = new(
            writeTemporaryFile: (path, _) =>
            {
                temporaryPath = path;
                File.WriteAllText(
                    path,
                    "partial temporary data");

                throw new IOException(
                    "Simulated temporary-write failure.");
            });
        ConsoleSessionRunner runner = new(fileWriter);
        StringWriter output = new();

        runner.SaveSession(
            output,
            temporary.SavePath,
            session);

        Assert.Equal(
            originalContents,
            File.ReadAllBytes(temporary.SavePath));
        Assert.NotNull(temporaryPath);
        Assert.Equal(
            temporary.DirectoryPath,
            Path.GetDirectoryName(temporaryPath));
        Assert.False(File.Exists(temporaryPath));
        Assert.Contains(
            $"Save failed: unable to write {temporary.SavePath}.",
            output.ToString());
        Assert.DoesNotContain(
            "Game saved to",
            output.ToString());
    }

    [Fact]
    public void SaveSession_WhenReplacementFails_PreservesDestination()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            WatchtowerScenarioSessionFactory.CreateNew(RandomSeed);
        byte[] originalContents = "existing valid save"u8.ToArray();
        File.WriteAllBytes(
            temporary.SavePath,
            originalContents);
        string? temporaryPath = null;
        string? temporaryContents = null;
        AtomicManualSaveFileWriter fileWriter = new(
            replaceTemporaryFile: (path, _) =>
            {
                temporaryPath = path;
                temporaryContents = File.ReadAllText(path);

                throw new IOException(
                    "Simulated replacement failure.");
            });
        ConsoleSessionRunner runner = new(fileWriter);
        StringWriter output = new();

        runner.SaveSession(
            output,
            temporary.SavePath,
            session);

        Assert.Equal(
            originalContents,
            File.ReadAllBytes(temporary.SavePath));
        Assert.NotNull(temporaryPath);
        Assert.Equal(
            temporary.DirectoryPath,
            Path.GetDirectoryName(temporaryPath));
        Assert.False(File.Exists(temporaryPath));
        Assert.Equal(
            ManualSaveSerializer.Serialize(session),
            temporaryContents);
        Assert.Contains(
            $"Save failed: unable to write {temporary.SavePath}.",
            output.ToString());
        Assert.DoesNotContain(
            "Game saved to",
            output.ToString());
    }

    [Fact]
    public void SaveSession_WhenSavingIsUnavailable_PerformsNoFileOperation()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            CreateRegionalTravelSession();
        byte[] originalContents = "existing valid save"u8.ToArray();
        File.WriteAllBytes(
            temporary.SavePath,
            originalContents);
        bool fileOperationAttempted = false;
        AtomicManualSaveFileWriter fileWriter = new(
            writeTemporaryFile: (_, _) =>
            {
                fileOperationAttempted = true;
                throw new InvalidOperationException(
                    "The file writer should not be invoked.");
            },
            replaceTemporaryFile: (_, _) =>
            {
                fileOperationAttempted = true;
                throw new InvalidOperationException(
                    "The file writer should not be invoked.");
            },
            deleteTemporaryFile: _ =>
            {
                fileOperationAttempted = true;
                throw new InvalidOperationException(
                    "The file writer should not be invoked.");
            });
        ConsoleSessionRunner runner = new(fileWriter);
        StringWriter output = new();

        runner.SaveSession(
            output,
            temporary.SavePath,
            session);

        Assert.False(fileOperationAttempted);
        Assert.Equal(
            originalContents,
            File.ReadAllBytes(temporary.SavePath));
        Assert.Equal(
            "Save failed: manual saving is no longer available."
                + Environment.NewLine,
            output.ToString());
        Assert.Equal(
            [temporary.SavePath],
            Directory.GetFiles(temporary.DirectoryPath));
    }

    [Fact]
    public void SaveSession_WithSuccessfulWrite_DoesNotChangeSession()
    {
        using TemporaryDirectory temporary = new();
        ApplicationSessionState session =
            WatchtowerScenarioSessionFactory.CreateNew(RandomSeed);
        var party = session.Party;
        var scenario = session.Scenario;
        string serializedBefore =
            ManualSaveSerializer.Serialize(session);
        ConsoleSessionRunner runner = new();

        runner.SaveSession(
            new StringWriter(),
            temporary.SavePath,
            session);

        Assert.Same(party, session.Party);
        Assert.Same(scenario, session.Scenario);
        Assert.Equal(
            serializedBefore,
            ManualSaveSerializer.Serialize(session));
    }

    [Fact]
    public void WriteAllText_WhenCleanupFails_PreservesOriginalFailure()
    {
        using TemporaryDirectory temporary = new();
        IOException replacementFailure = new(
            "Simulated replacement failure.");
        string? temporaryPath = null;
        AtomicManualSaveFileWriter fileWriter = new(
            replaceTemporaryFile: (path, _) =>
            {
                temporaryPath = path;
                throw replacementFailure;
            },
            deleteTemporaryFile: _ =>
                throw new IOException(
                    "Simulated cleanup failure."));

        try
        {
            IOException actual = Assert.Throws<IOException>(() =>
                fileWriter.WriteAllText(
                    temporary.SavePath,
                    "new data"));

            Assert.Same(replacementFailure, actual);
            Assert.NotNull(temporaryPath);
            Assert.True(File.Exists(temporaryPath));
        }
        finally
        {
            if (temporaryPath is not null)
            {
                File.Delete(temporaryPath);
            }
        }
    }

    [Fact]
    public void WriteAllText_RepeatedFailuresUseUniqueTemporaryPaths()
    {
        using TemporaryDirectory temporary = new();
        List<string> temporaryPaths = new();
        AtomicManualSaveFileWriter fileWriter = new(
            writeTemporaryFile: (path, _) =>
            {
                temporaryPaths.Add(path);
                throw new IOException(
                    "Simulated temporary-write failure.");
            });

        _ = Assert.Throws<IOException>(() =>
            fileWriter.WriteAllText(
                temporary.SavePath,
                "first"));
        _ = Assert.Throws<IOException>(() =>
            fileWriter.WriteAllText(
                temporary.SavePath,
                "second"));

        Assert.Equal(2, temporaryPaths.Count);
        Assert.NotEqual(
            temporaryPaths[0],
            temporaryPaths[1]);
        Assert.All(
            temporaryPaths,
            path => Assert.Equal(
                temporary.DirectoryPath,
                Path.GetDirectoryName(path)));
    }

    private static ApplicationSessionState
        CreateRegionalTravelSession()
    {
        ApplicationSessionState session =
            WatchtowerScenarioSessionFactory.CreateNew(RandomSeed);
        ApplicationSessionState accepted =
            OutpostMissionRules.Resolve(
                session,
                OutpostMissionChoice.AcceptMission)
            .State;

        return RegionalTravelRules.BeginWatchtowerJourney(
            accepted);
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
