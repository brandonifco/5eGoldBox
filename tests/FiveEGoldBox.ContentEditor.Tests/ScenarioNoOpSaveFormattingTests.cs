using FiveEGoldBox.ContentEditor.Models;
using FiveEGoldBox.ContentEditor.Services;

namespace FiveEGoldBox.ContentEditor.Tests;

/// The scenario-content equivalent of NoOpSaveFormattingTests: a save that
/// changes nothing must produce a byte-identical file, run against a temp
/// copy of each real committed data/scenarios/*/scenario.json -- never the
/// committed files themselves. This is the proof that ScenarioJsonFormatting
/// actually reproduces real hand-authored content, not just
/// plausible-looking content.
///
/// Saving a location leaves every map's original bytes untouched; saving a
/// map re-renders it. Both are byte-identical for all three files now that
/// Hollow Mill's floor field order has been normalized to the renderer's own
/// (see ScenarioJsonFormatting's header and CommittedScenarioMapNormalizer) --
/// before that, Hollow Mill was excluded here because re-rendering it moved
/// a block, which is exactly the kind of silent noise these tests exist to
/// catch rather than tolerate.
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

    /// Watchtower and Sunken Chapel already write each floor's fields in the
    /// canonical order RenderExplorationMap emits, so re-rendering their maps
    /// has to reproduce them exactly. This is the real proof the map renderer
    /// is faithful rather than merely plausible -- it covers every shape in
    /// committed content: multi-floor and single-floor maps, an empty Stairs
    /// array, secret and locked doors, a treasure carrying gold plus an item
    /// and quantity, and an NPC.
    [Theory]
    [InlineData("watchtower")]
    [InlineData("sunken-chapel")]
    [InlineData("hollow-mill")]
    public void ResavingEveryExistingMapOneAtATimeProducesAByteIdenticalFile(
        string scenarioDirectoryName)
    {
        string realFilePath = RepositoryLocator.ResolveScenarioPackPaths()
            .Single(path => path.Contains(scenarioDirectoryName));
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            ScenarioContentService service = new();
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var location in service.LoadLocations(tempFile))
            {
                var map = service.FindExplorationMap(tempFile, location.LocationId);
                if (map is null)
                {
                    continue;
                }

                var result = service.SaveExplorationMap(tempFile, location.LocationId, map);
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

    /// Every committed encounter carries an empty BlockedPositions, so that
    /// inline [] shape is proven here; the non-empty case has no committed
    /// example to check against and is covered structurally instead (see
    /// ScenarioContentServiceTests).
    [Theory]
    [MemberData(nameof(RealScenarioFilePaths))]
    public void ResavingEveryExistingEncounterOneAtATimeProducesAByteIdenticalFile(
        string realFilePath)
    {
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            ScenarioContentService service = new();
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var encounter in service.LoadEncounters(tempFile))
            {
                var result = service.SaveEncounter(tempFile, encounter);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            AssertByteIdentical(before, File.ReadAllBytes(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// Covers the optional-property shapes that only appear in real content:
    /// a trigger with no EncounterId (Hollow Mill's non-combat triggers) and
    /// one with all of Floor/Position/EncounterId set (Watchtower's ambush).
    /// Writing an absent optional as null rather than omitting it would fail
    /// here rather than quietly changing every trigger in the file.
    [Theory]
    [MemberData(nameof(RealScenarioFilePaths))]
    public void ResavingEveryExistingTriggerOneAtATimeProducesAByteIdenticalFile(
        string realFilePath)
    {
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            ScenarioContentService service = new();
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var trigger in service.LoadTriggers(tempFile))
            {
                var result = service.SaveTrigger(tempFile, trigger);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            AssertByteIdentical(before, File.ReadAllBytes(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// Decisions nest a variable-length Options array whose entries have their
    /// own optional ResultingProgressId -- a declining option ("Not yet")
    /// legitimately advances nothing and must write no property at all.
    [Theory]
    [MemberData(nameof(RealScenarioFilePaths))]
    public void ResavingEveryExistingDecisionOneAtATimeProducesAByteIdenticalFile(
        string realFilePath)
    {
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            ScenarioContentService service = new();
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var decision in service.LoadDecisions(tempFile))
            {
                var result = service.SaveDecision(tempFile, decision);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            AssertByteIdentical(before, File.ReadAllBytes(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// The grid editor round-trips a map through ExplorationMapFormModel, not
    /// through the DTO directly, so this covers the failure the other map
    /// tests can't see: TraversablePositions' hand-authored cell order is not
    /// purely row-major, and a form model backed by an unordered set would
    /// silently rewrite every untouched floor's cell order on first save.
    /// hollow-mill matters specifically: it holds the only gold-only treasure
    /// in committed content (no ItemId, no Quantity), which is the one case
    /// where the form model's empty-string-to-null normalization has to be
    /// right or the save writes "ItemId": "" where the property should be
    /// absent entirely.
    [Theory]
    [InlineData("watchtower")]
    [InlineData("sunken-chapel")]
    [InlineData("hollow-mill")]
    public void RoundTrippingAMapThroughTheFormModelProducesAByteIdenticalFile(
        string scenarioDirectoryName)
    {
        string realFilePath = RepositoryLocator.ResolveScenarioPackPaths()
            .Single(path => path.Contains(scenarioDirectoryName));
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            ScenarioContentService service = new();
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var location in service.LoadLocations(tempFile))
            {
                var map = service.FindExplorationMap(tempFile, location.LocationId);
                if (map is null)
                {
                    continue;
                }

                var roundTripped = ExplorationMapFormModel.FromDefinition(map).ToDefinition();
                var result = service.SaveExplorationMap(
                    tempFile,
                    location.LocationId,
                    roundTripped);

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
