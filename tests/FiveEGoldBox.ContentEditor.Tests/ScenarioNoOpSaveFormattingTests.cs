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
/// Saving a location leaves every map's original bytes untouched, so those
/// tests hold for all three files. Saving a *map* re-renders it, which is
/// byte-identical for the two files already written in the canonical field
/// order and a deliberate normalization for Hollow Mill -- see
/// ResavingAHollowMillMapNormalizesFloorFieldOrderWithoutChangingContent.
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

    /// Hollow Mill's ground floor writes Npcs before Doors/Treasures, which
    /// no single canonical order can reproduce alongside Watchtower's
    /// opposite ordering. Re-rendering its map therefore normalizes that
    /// ordering -- an accepted, deliberate change (see
    /// ScenarioJsonFormatting's header). What must NOT change is the content
    /// itself, which is what this asserts: same cells, stairs, doors,
    /// treasures and NPCs, and a file that still validates.
    [Fact]
    public void ResavingAHollowMillMapNormalizesFloorFieldOrderWithoutChangingContent()
    {
        string realFilePath = RepositoryLocator.ResolveScenarioPackPaths()
            .Single(path => path.Contains("hollow-mill"));
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            ScenarioContentService service = new();
            string locationId = service.LoadLocations(tempFile)
                .First(location => location.ExplorationMap is not null)
                .LocationId;

            var before = service.FindExplorationMap(tempFile, locationId);
            Assert.NotNull(before);

            var result = service.SaveExplorationMap(tempFile, locationId, before);
            Assert.True(result.IsValid, DescribeIssues(result));

            var after = service.FindExplorationMap(tempFile, locationId);
            Assert.NotNull(after);

            Assert.Equal(before.MapId, after.MapId);
            Assert.Equal(before.Width, after.Width);
            Assert.Equal(before.Height, after.Height);
            Assert.Equal(before.StartingFloor, after.StartingFloor);
            Assert.Equal(before.StartingFacing, after.StartingFacing);
            Assert.Equal(before.Floors.Count, after.Floors.Count);

            for (int i = 0; i < before.Floors.Count; i++)
            {
                var expectedFloor = before.Floors[i];
                var actualFloor = after.Floors[i];

                Assert.Equal(expectedFloor.Floor, actualFloor.Floor);
                Assert.Equal(
                    expectedFloor.TraversablePositions.Select(p => (p.X, p.Y)),
                    actualFloor.TraversablePositions.Select(p => (p.X, p.Y)));
                Assert.Equal(
                    expectedFloor.Stairs.Select(s => (s.Position.X, s.Position.Y, s.DestinationFloor)),
                    actualFloor.Stairs.Select(s => (s.Position.X, s.Position.Y, s.DestinationFloor)));
                Assert.Equal(
                    expectedFloor.Doors.Select(d => (d.DoorId, d.Side, d.IsSecret, d.IsLocked)),
                    actualFloor.Doors.Select(d => (d.DoorId, d.Side, d.IsSecret, d.IsLocked)));
                Assert.Equal(
                    expectedFloor.Treasures.Select(t => (t.TreasureId, t.GoldPieces, t.ItemId, t.Quantity)),
                    actualFloor.Treasures.Select(t => (t.TreasureId, t.GoldPieces, t.ItemId, t.Quantity)));
                Assert.Equal(
                    expectedFloor.Npcs.Select(n => (n.NpcId, n.Name, n.DialogueText)),
                    actualFloor.Npcs.Select(n => (n.NpcId, n.Name, n.DialogueText)));
            }
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
    [Theory]
    [InlineData("watchtower")]
    [InlineData("sunken-chapel")]
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
