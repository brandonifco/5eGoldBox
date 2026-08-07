using FiveEGoldBox.ContentEditor.Services;

namespace FiveEGoldBox.ContentEditor.Tests;

/// The campaign equivalent of ScenarioNoOpSaveFormattingTests: re-saving
/// every roster member unchanged must leave the file byte-identical, run
/// against a temp copy of the real committed campaign.json.
///
/// This is the proof that the three omit-rather-than-default rules are right.
/// TemporaryHitPoints is the dangerous one -- a non-nullable int that only
/// the Fighter carries, so a renderer that wrote its default would silently
/// add "TemporaryHitPoints": 0 to five of six roster entries and this test
/// is what catches that.
public sealed class CampaignNoOpSaveFormattingTests
{
    public static IEnumerable<object[]> RealCampaignFilePaths()
    {
        return RepositoryLocator.ResolveCampaignPackPaths()
            .Select(path => new object[] { path });
    }

    [Theory]
    [MemberData(nameof(RealCampaignFilePaths))]
    public void ResavingEveryRosterMemberOneAtATimeProducesAByteIdenticalFile(
        string realFilePath)
    {
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            CampaignContentService service = new();
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var member in service.LoadRoster(tempFile))
            {
                var result = service.SaveRosterMember(tempFile, member);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            ScenarioNoOpSaveFormattingTests.AssertByteIdentical(
                before,
                File.ReadAllBytes(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// Guards the specific shapes the byte-identity test would also catch but
    /// would report only as "line N differs" -- named here so a regression
    /// says which convention broke.
    [Fact]
    public void EveryOptionalPropertyIsOmittedRatherThanWrittenWithItsDefault()
    {
        string realFilePath = RepositoryLocator.ResolveCampaignPackPaths().Single();
        string tempFile = CopyToTempFile(realFilePath);

        try
        {
            CampaignContentService service = new();

            foreach (var member in service.LoadRoster(tempFile))
            {
                Assert.True(service.SaveRosterMember(tempFile, member).IsValid);
            }

            string json = File.ReadAllText(tempFile);

            // Only the Fighter carries temporary hit points.
            Assert.Single(
                json.Split("\"TemporaryHitPoints\"").Skip(1).ToList());

            // Ammunition on the two bow carriers, prepared spells on the two
            // casters -- and nothing written for anyone else.
            Assert.Equal(2, json.Split("\"Ammunition\"").Length - 1);
            Assert.Equal(2, json.Split("\"PreparedSpellIds\"").Length - 1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static string CopyToTempFile(
        string realFilePath)
    {
        string tempFile = Path.Combine(
            Path.GetTempPath(),
            $"campaign-formatting-{Guid.NewGuid():N}.json");

        File.Copy(realFilePath, tempFile);
        return tempFile;
    }

    private static string DescribeIssues(
        FiveEGoldBox.Core.Validation.ValidationResult result)
    {
        return string.Join(
            "; ",
            result.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
    }
}
