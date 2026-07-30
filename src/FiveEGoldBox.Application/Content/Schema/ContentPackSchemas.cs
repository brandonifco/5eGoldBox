using System.Text.Json;
using System.Text.Json.Nodes;
using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.Application.Content.Schema;

/// The three JSON Schema documents an editor uses for autocomplete and
/// inline validation while hand-authoring a content pack, one per pack root.
/// Each is derived from the same V1 DTOs the real loaders deserialize into,
/// via JsonSchemaGenerator, so a schema can only go stale by a DTO changing
/// without ContentPackSchemaWriter being re-run -- exactly the drift
/// ContentPackSchemaTests checks for.
internal static class ContentPackSchemas
{
    // Derived from Default rather than a bare `new()` so it inherits a
    // populated TypeInfoResolver -- a JsonSerializerOptions used for
    // serialization needs one, and only Default guarantees it is set.
    private static readonly JsonSerializerOptions
        WriteOptions = new(JsonSerializerOptions.Default)
        {
            WriteIndented = true
        };

    internal static string RulesetPackSchema()
    {
        return Serialize(JsonSchemaGenerator.Generate(
            typeof(RulesetPackV1),
            "5eGoldBox ruleset content pack"));
    }

    internal static string ScenarioPackSchema()
    {
        return Serialize(JsonSchemaGenerator.Generate(
            typeof(ScenarioPackV1),
            "5eGoldBox scenario content pack"));
    }

    internal static string CampaignPackSchema()
    {
        return Serialize(JsonSchemaGenerator.Generate(
            typeof(CampaignPackV1),
            "5eGoldBox campaign content pack"));
    }

    private static string Serialize(
        JsonObject schema)
    {
        return schema.ToJsonString(WriteOptions) + Environment.NewLine;
    }
}
