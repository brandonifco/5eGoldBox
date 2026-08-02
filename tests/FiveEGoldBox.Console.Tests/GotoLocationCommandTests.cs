namespace FiveEGoldBox.Console.Tests;

public sealed class GotoLocationCommandTests
{
    [Fact]
    public void Run_WithTooFewArguments_PrintsUsageAndReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = GotoLocationCommand.Run(
            ["scenario.watchtower", "location.ruined-watchtower", "GroundFloor", "2", "1"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage:", error.ToString());
        Assert.Empty(output.ToString());
    }

    [Fact]
    public void Run_WithTooManyArguments_PrintsUsageAndReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = GotoLocationCommand.Run(
            ["scenario.watchtower", "location.ruined-watchtower", "GroundFloor", "2", "1", "East", "MissionAccepted", "extra"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage:", error.ToString());
    }

    [Fact]
    public void Run_WithAMalformedXCoordinate_ReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = GotoLocationCommand.Run(
            ["scenario.watchtower", "location.ruined-watchtower", "GroundFloor", "not-a-number", "1", "East"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("X coordinate", error.ToString());
    }

    [Fact]
    public void Run_WithAMalformedYCoordinate_ReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = GotoLocationCommand.Run(
            ["scenario.watchtower", "location.ruined-watchtower", "GroundFloor", "2", "not-a-number", "East"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Y coordinate", error.ToString());
    }

    [Fact]
    public void Run_WithAnUnknownFacing_ReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = GotoLocationCommand.Run(
            ["scenario.watchtower", "location.ruined-watchtower", "GroundFloor", "2", "1", "Sideways"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("facing", error.ToString());
    }

    [Fact]
    public void Run_WithAnUnknownScenario_ReturnsOneAndDoesNotThrow()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = GotoLocationCommand.Run(
            ["scenario.does-not-exist", "location.ruined-watchtower", "GroundFloor", "2", "1", "East"],
            output,
            error);

        Assert.Equal(1, exitCode);
        Assert.NotEmpty(error.ToString());
    }

    [Fact]
    public void Run_WithANonTraversablePosition_ReturnsOneAndDoesNotThrow()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = GotoLocationCommand.Run(
            ["scenario.watchtower", "location.ruined-watchtower", "GroundFloor", "99", "99", "East", "MissionAccepted"],
            output,
            error);

        Assert.Equal(1, exitCode);
        Assert.NotEmpty(error.ToString());
    }

    [Fact]
    public async Task Main_WithGotoArguments_WiresThemThroughTheRealProcessAtTheArmoryDoor()
    {
        string workingDirectory = Directory.CreateTempSubdirectory(
            "FiveEGoldBox.Console.GotoProcessTests.").FullName;

        try
        {
            ConsoleProcessResult result = await ConsoleProcessHarness.RunAsync(
                workingDirectory,
                scriptedInput: string.Empty,
                arguments:
                [
                    "goto",
                    "scenario.watchtower",
                    "location.ruined-watchtower",
                    "GroundFloor",
                    "2",
                    "1",
                    "East",
                    "MissionAccepted"
                ]);

            Assert.True(
                !result.TimedOut && result.ExitCode == 0,
                result.CreateFailureDiagnostic(
                    Path.Combine(workingDirectory, "savegame.json")));
            Assert.Contains("Mode: Exploration", result.StandardOutput);
            Assert.Contains("Map ID: map.ruined-watchtower", result.StandardOutput);
            Assert.Contains("Floor: GroundFloor", result.StandardOutput);
            Assert.Contains("Position X: 2", result.StandardOutput);
            Assert.Contains("Position Y: 1", result.StandardOutput);
            Assert.Contains("Facing: East", result.StandardOutput);
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }
}
