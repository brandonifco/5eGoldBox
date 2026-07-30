using FiveEGoldBox.Application.Content;

namespace FiveEGoldBox.Console.Tests;

public sealed class ContentPackValidationCommandTests
{
    [Fact]
    public void Run_WithTooFewArguments_PrintsUsageAndReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = ContentPackValidationCommand.Run(
            ["ruleset"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage:", error.ToString());
        Assert.Empty(output.ToString());
    }

    [Fact]
    public void Run_WithAnUnknownKind_ReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = ContentPackValidationCommand.Run(
            ["spellbook", "path.json"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown content kind", error.ToString());
    }

    [Fact]
    public void Run_WithTwoScenarioPaths_ReturnsTwo()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = ContentPackValidationCommand.Run(
            ["scenario", "a.json", "b.json"],
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("single file", error.ToString());
    }

    [Fact]
    public void Run_WithTheRealCampaignRuleset_ReportsValidAndReturnsZero()
    {
        string path = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("rulesets", "campaign", "core.json"));
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = ContentPackValidationCommand.Run(
            ["ruleset", path],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("Valid: no issues found.", output.ToString());
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void Run_WithARealScenario_ReportsValidAndReturnsZero()
    {
        string path = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("scenarios", "hollow-mill", "scenario.json"));
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = ContentPackValidationCommand.Run(
            ["scenario", path],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("Valid: no issues found.", output.ToString());
    }

    [Fact]
    public void Run_WithARealCampaign_ReportsValidAndReturnsZero()
    {
        string path = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("campaigns", "frontier", "campaign.json"));
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = ContentPackValidationCommand.Run(
            ["campaign", path],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("Valid: no issues found.", output.ToString());
    }

    [Fact]
    public void Run_WithAMalformedPack_ReportsTheIssueAndReturnsOne()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "{ not valid json");

            StringWriter output = new();
            StringWriter error = new();

            int exitCode = ContentPackValidationCommand.Run(
                ["ruleset", path],
                output,
                error);

            Assert.Equal(1, exitCode);
            Assert.Contains("content.pack.parse_failed", output.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Main_WithValidateArguments_WiresThemThroughTheRealProcess()
    {
        string path = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("rulesets", "campaign", "core.json"));
        string workingDirectory = Directory.CreateTempSubdirectory(
            "FiveEGoldBox.Console.ValidateProcessTests.").FullName;

        try
        {
            ConsoleProcessResult result = await ConsoleProcessHarness.RunAsync(
                workingDirectory,
                scriptedInput: string.Empty,
                arguments: ["validate", "ruleset", path]);

            Assert.True(
                !result.TimedOut && result.ExitCode == 0,
                result.CreateFailureDiagnostic(
                    Path.Combine(workingDirectory, "savegame.json")));
            Assert.Contains("Valid: no issues found.", result.StandardOutput);
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }
}
