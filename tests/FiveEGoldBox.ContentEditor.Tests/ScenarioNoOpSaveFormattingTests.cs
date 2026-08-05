using FiveEGoldBox.ContentEditor.Services;

namespace FiveEGoldBox.ContentEditor.Tests;

/// The scenario-content equivalent of NoOpSaveFormattingTests: a save that
/// changes nothing must produce a byte-identical file, run against a temp
/// copy of each real committed data/scenarios/*/scenario.json -- never the
/// committed files themselves. This is the proof that ScenarioJsonFormatting
/// (including its full ExplorationMap renderer, needed only so an unrelated
/// location's untouched map round-trips faithfully) actually reproduces
/// real hand-authored content, not just plausible-looking content.
public sealed class ScenarioNoOpSaveFormattingTests
{
    public static IEnumerable<object[]> RealScenarioFilePaths()
    {
        return RepositoryLocator.ResolveScenarioPackPaths()
            .Select(path => new object[] { path });
    }

    [Theory]
    [MemberData(nameof(RealScenarioFilePaths))]
    public void ResavingEveryExistingLocationOneAtATimeProducesAByteIdenticalFile(
        string realFilePath)
    {
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            ScenarioContentService service = new();
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var location in service.LoadLocations(tempFile))
            {
                var result = service.SaveLocation(tempFile, location);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            byte[] after = File.ReadAllBytes(tempFile);
            AssertByteIdentical(before, after);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [MemberData(nameof(RealScenarioFilePaths))]
    public void ResavingEveryExistingRouteOneAtATimeProducesAByteIdenticalFile(
        string realFilePath)
    {
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            ScenarioContentService service = new();
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var route in service.LoadRoutes(tempFile))
            {
                var result = service.SaveRoute(tempFile, route);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            byte[] after = File.ReadAllBytes(tempFile);
            AssertByteIdentical(before, after);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ResavingEveryExistingShopOneAtATimeProducesAByteIdenticalFile()
    {
        // Only Hollow Mill has any shops today -- Watchtower/Sunken Chapel
        // have none, which exercises ReplaceOrInsertRootPropertyValue's
        // insert path (see the dedicated test for that below), not the
        // plain replace path this test covers.
        string realFilePath = RepositoryLocator.ResolveScenarioPackPaths()
            .Single(path => path.Contains("hollow-mill"));
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            ScenarioContentService service = new();
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var shop in service.LoadShops(tempFile))
            {
                var result = service.SaveShop(tempFile, shop);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            byte[] after = File.ReadAllBytes(tempFile);
            AssertByteIdentical(before, after);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    internal static void AssertByteIdentical(
        byte[] expected,
        byte[] actual)
    {
        if (expected.AsSpan().SequenceEqual(actual))
        {
            return;
        }

        string expectedText = System.Text.Encoding.UTF8.GetString(expected);
        string actualText = System.Text.Encoding.UTF8.GetString(actual);

        string[] expectedLines = expectedText.Split('\n');
        string[] actualLines = actualText.Split('\n');

        for (int i = 0; i < Math.Min(expectedLines.Length, actualLines.Length); i++)
        {
            if (expectedLines[i] != actualLines[i])
            {
                Assert.Fail(
                    $"First differing line {i + 1}:\nExpected: {expectedLines[i]}\nActual:   {actualLines[i]}");
            }
        }

        Assert.Fail(
            $"Byte content differs (expected {expected.Length} bytes, actual {actual.Length} bytes) but no differing line found within the shorter length.");
    }

    internal static string DescribeIssues(
        FiveEGoldBox.Core.Validation.ValidationResult result)
    {
        return string.Join(
            "\n",
            result.Issues.Select(issue => $"{issue.Severity} {issue.Code}: {issue.Message}"));
    }

    internal static string CopyToTempFile(
        string realFilePath)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"scenario-{Guid.NewGuid():N}.json");
        File.Copy(realFilePath, tempPath);
        return tempPath;
    }
}
