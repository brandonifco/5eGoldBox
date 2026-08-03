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

    /// The load-time cross-pack check that a combatant's MonsterId resolves
    /// against the scenario's own ruleset -- new behaviour, and this is the
    /// path a real content author actually hits: the standalone `validate`
    /// command, which this facade backs.
    [Fact]
    public void ValidateScenarioPack_WithAnUnresolvedMonsterId_ReportsTheValidationIssue()
    {
        const string packWithUnresolvedMonster = """
            {
                "FormatVersion": 1,
                "ScenarioId": "scenario.test",
                "DisplayName": "Test",
                "RulesetId": "ruleset.campaign",
                "StartingLocationId": "location.hub",
                "Progress": {
                    "InitialProgressId": "test.not_started",
                    "ProgressIds": [ "test.not_started", "test.won", "test.lost" ],
                    "Conclusions": [
                        { "ProgressId": "test.won", "IsSuccess": true, "LocationId": "location.hub", "EpilogueText": "Won." },
                        { "ProgressId": "test.lost", "IsSuccess": false, "LocationId": "location.hub", "EpilogueText": "Lost." }
                    ]
                },
                "PartyRequirement": {
                    "MinimumMembers": 1,
                    "MaximumMembers": 1,
                    "MinimumConsciousMembers": 1
                },
                "Locations": [
                    { "LocationId": "location.hub", "DisplayName": "Hub" }
                ],
                "Routes": [],
                "Encounters": [
                    {
                        "EncounterId": "encounter.test",
                        "BattlefieldId": "battlefield.test",
                        "Width": 4,
                        "Height": 4,
                        "PartySideId": "side.party",
                        "BlockedPositions": [],
                        "PartyStartingPositions": [ { "X": 0, "Y": 0 } ],
                        "Combatants": [
                            {
                                "CombatantId": "combatant.foe",
                                "MonsterId": "monster.does-not-exist",
                                "SideId": "side.enemy",
                                "StartingPosition": { "X": 3, "Y": 3 }
                            }
                        ],
                        "Outcome": {
                            "VictoryProgressId": "test.won",
                            "DefeatProgressId": "test.lost"
                        }
                    }
                ],
                "Triggers": [],
                "Decisions": []
            }
            """;

        string path = WriteTempFile(packWithUnresolvedMonster);

        try
        {
            ValidationResult result = ContentPackValidation.ValidateScenarioPack(
                path);

            Assert.False(result.IsValid);
            Assert.Contains(
                result.Issues,
                issue => issue.Code
                    == "scenario.combatants.monster_id_unresolved");
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
