using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Tests;

public sealed class ContentPackValidationTests
{
    [Fact]
    public void ValidateRulesetPack_WithTheRealCampaignRuleset_HasNoIssues()
    {
        string path = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("rulesets", "campaign", "core.json"));

        ValidationResult result = ContentPackValidation.ValidateRulesetPack(
            [path]);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Theory]
    [InlineData("watchtower")]
    [InlineData("sunken-chapel")]
    [InlineData("hollow-mill")]
    public void ValidateScenarioPack_WithARealScenario_HasNoIssues(
        string scenarioDirectoryName)
    {
        string path = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("scenarios", scenarioDirectoryName, "scenario.json"));

        ValidationResult result = ContentPackValidation.ValidateScenarioPack(
            path);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidateCampaignPack_WithTheRealFrontierCampaign_HasNoIssues()
    {
        string path = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("campaigns", "frontier", "campaign.json"));

        ValidationResult result = ContentPackValidation.ValidateCampaignPack(
            path);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidateRulesetPack_WithMalformedJson_ReportsAParseFailureIssue()
    {
        string path = WriteTempFile("{ not valid json");

        try
        {
            ValidationResult result = ContentPackValidation.ValidateRulesetPack(
                [path]);

            Assert.False(result.IsValid);
            ValidationIssue issue = Assert.Single(result.Issues);
            Assert.Equal("content.pack.parse_failed", issue.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ValidateRulesetPack_WithAMissingFile_ReportsAParseFailureIssue()
    {
        string missingPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.json");

        ValidationResult result = ContentPackValidation.ValidateRulesetPack(
            [missingPath]);

        Assert.False(result.IsValid);
        ValidationIssue issue = Assert.Single(result.Issues);
        Assert.Equal("content.pack.parse_failed", issue.Code);
    }

    [Fact]
    public void ValidateRulesetPack_WithADuplicateRaceId_ReportsTheStructuralIssue()
    {
        const string packWithDuplicateRaceIds = """
            {
                "FormatVersion": 1,
                "Id": "ruleset.test",
                "Name": "Test Ruleset",
                "Races": [
                    { "Id": "race.duplicate", "Name": "One", "BaseSpeedFeet": 30 },
                    { "Id": "race.duplicate", "Name": "Two", "BaseSpeedFeet": 30 }
                ]
            }
            """;

        string path = WriteTempFile(packWithDuplicateRaceIds);

        try
        {
            ValidationResult result = ContentPackValidation.ValidateRulesetPack(
                [path]);

            Assert.False(result.IsValid);
            Assert.Contains(
                result.Issues,
                issue => issue.Code == "ruleset.races.duplicate_id");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ValidateScenarioPack_WithAnUnsupportedFormatVersion_ReportsAParseFailureIssue()
    {
        const string futureVersionPack = """
            {
                "FormatVersion": 2,
                "ScenarioId": "scenario.test",
                "DisplayName": "Test",
                "RulesetId": "ruleset.test",
                "StartingLocationId": "location.hub",
                "Progress": {
                    "InitialProgressId": "test.not_started",
                    "ProgressIds": [ "test.not_started" ],
                    "Conclusions": []
                },
                "PartyRequirement": {
                    "MinimumMembers": 1,
                    "MaximumMembers": 4,
                    "MinimumConsciousMembers": 1
                },
                "Locations": [],
                "Routes": [],
                "Encounters": [],
                "Triggers": [],
                "Decisions": []
            }
            """;

        string path = WriteTempFile(futureVersionPack);

        try
        {
            ValidationResult result = ContentPackValidation.ValidateScenarioPack(
                path);

            Assert.False(result.IsValid);
            ValidationIssue issue = Assert.Single(result.Issues);
            Assert.Equal("content.pack.parse_failed", issue.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ValidateCampaignPack_WithNoActivePartySize_ReportsTheValidationIssue()
    {
        const string emptyPartyCampaign = """
            {
                "FormatVersion": 1,
                "CampaignId": "campaign.test",
                "DisplayName": "Test Campaign",
                "RulesetId": "ruleset.test",
                "ActivePartySize": 0,
                "Roster": [],
                "ScenarioIds": []
            }
            """;

        string path = WriteTempFile(emptyPartyCampaign);

        try
        {
            ValidationResult result = ContentPackValidation.ValidateCampaignPack(
                path);

            Assert.False(result.IsValid);
            ValidationIssue issue = Assert.Single(result.Issues);
            Assert.Equal("content.campaign.invalid", issue.Code);
            Assert.Contains("at least one character", issue.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTempFile(
        string content)
    {
        string path = Path.GetTempFileName();

        File.WriteAllText(path, content);

        return path;
    }
}
