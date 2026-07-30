using System.Text.Json.Nodes;
using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Application.Content.Schema;

namespace FiveEGoldBox.Application.Tests;

/// Checks the generated schemas against what is actually checked in under
/// data/schemas/. A mismatch here means a V1 DTO changed without
/// ContentPackSchemaWriter being re-run -- the same drift discipline the
/// frozen combat transcripts already use, applied to a generated artifact
/// instead of a hand-verified one.
public sealed class ContentPackSchemaTests
{
    [Fact]
    public void RulesetPackSchema_MatchesTheCheckedInFile()
    {
        AssertMatchesCheckedInFile(
            "ruleset-pack.schema.json",
            ContentPackSchemas.RulesetPackSchema());
    }

    [Fact]
    public void ScenarioPackSchema_MatchesTheCheckedInFile()
    {
        AssertMatchesCheckedInFile(
            "scenario-pack.schema.json",
            ContentPackSchemas.ScenarioPackSchema());
    }

    [Fact]
    public void CampaignPackSchema_MatchesTheCheckedInFile()
    {
        AssertMatchesCheckedInFile(
            "campaign-pack.schema.json",
            ContentPackSchemas.CampaignPackSchema());
    }

    [Theory]
    [InlineData("ruleset-pack.schema.json")]
    [InlineData("scenario-pack.schema.json")]
    [InlineData("campaign-pack.schema.json")]
    public void EveryRef_ResolvesToARealDefinition(
        string schemaFileName)
    {
        JsonObject schema = LoadCheckedInSchema(schemaFileName);
        JsonObject defs = schema["$defs"] as JsonObject
            ?? [];

        foreach (string reference in CollectRefs(schema))
        {
            const string prefix = "#/$defs/";
            Assert.StartsWith(prefix, reference);

            string defName = reference[prefix.Length..];
            Assert.True(
                defs.ContainsKey(defName),
                $"'{reference}' does not resolve to a definition in {schemaFileName}.");
        }
    }

    [Fact]
    public void RulesetPackSchema_RequiresOnlyFormatVersionAtTheRoot()
    {
        JsonObject schema = LoadCheckedInSchema("ruleset-pack.schema.json");
        JsonArray required = Assert.IsType<JsonArray>(schema["required"]);

        Assert.Equal(
            ["FormatVersion"],
            required.Select(node => node!.GetValue<string>()));
    }

    [Fact]
    public void RulesetPackSchema_DeclaresTheEnumValuesOfADiceKind()
    {
        JsonObject schema = LoadCheckedInSchema("ruleset-pack.schema.json");
        JsonObject defs = (JsonObject)schema["$defs"]!;
        JsonObject zeroHitPointPolicy = (JsonObject)((JsonObject)defs["MonsterDefinitionV1"]!)
            ["properties"]!["ZeroHitPointPolicy"]!;

        Assert.Equal(
            ["DeathSavingThrows", "Defeated"],
            ((JsonArray)zeroHitPointPolicy["enum"]!)
                .Select(node => node!.GetValue<string>()));
    }

    [Fact]
    public void CampaignPackSchema_ModelsAbilityScoresAsAnIntegerValuedObject()
    {
        JsonObject schema = LoadCheckedInSchema("campaign-pack.schema.json");
        JsonObject defs = (JsonObject)schema["$defs"]!;
        JsonObject abilityScores = (JsonObject)((JsonObject)defs["CampaignCharacterDefinitionV1"]!)
            ["properties"]!["AbilityScores"]!;

        Assert.Equal("object", abilityScores["type"]!.GetValue<string>());
        Assert.Equal(
            "integer",
            ((JsonObject)abilityScores["additionalProperties"]!)["type"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("ruleset-pack.schema.json")]
    [InlineData("scenario-pack.schema.json")]
    [InlineData("campaign-pack.schema.json")]
    public void EachSchema_AllowsAnInstanceSchemaProperty(
        string schemaFileName)
    {
        JsonObject schema = LoadCheckedInSchema(schemaFileName);
        JsonObject properties = (JsonObject)schema["properties"]!;

        Assert.True(properties.ContainsKey("$schema"));
    }

    private static IEnumerable<string> CollectRefs(
        JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                if (jsonObject.TryGetPropertyValue("$ref", out JsonNode? refNode)
                    && refNode is not null)
                {
                    yield return refNode.GetValue<string>();
                }

                foreach ((string key, JsonNode? value) in jsonObject)
                {
                    if (key == "$ref")
                    {
                        continue;
                    }

                    foreach (string nested in CollectRefs(value))
                    {
                        yield return nested;
                    }
                }

                break;

            case JsonArray jsonArray:
                foreach (JsonNode? item in jsonArray)
                {
                    foreach (string nested in CollectRefs(item))
                    {
                        yield return nested;
                    }
                }

                break;
        }
    }

    private static void AssertMatchesCheckedInFile(
        string fileName,
        string generated)
    {
        string checkedInPath = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("schemas", fileName));
        string checkedIn = File.ReadAllText(checkedInPath);

        Assert.Equal(checkedIn, generated);
    }

    private static JsonObject LoadCheckedInSchema(
        string fileName)
    {
        string path = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("schemas", fileName));

        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }
}
