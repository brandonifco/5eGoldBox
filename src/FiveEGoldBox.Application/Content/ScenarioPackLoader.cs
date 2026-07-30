using System.Text.Json;
using System.Text.Json.Serialization;
using FiveEGoldBox.Application.Content.V1;
using FiveEGoldBox.Application.Scenarios.Definitions;

namespace FiveEGoldBox.Application.Content;

/// Reads one scenario content pack from JSON.
///
/// Unlike RulesetPackLoader there is no merge step and no validate-or-throw
/// wrapper here -- ScenarioDefinitionRegistry already validates whatever its
/// factory returns (via ScenarioDefinitionValidator) before caching it, so
/// this only needs to get from disk to a raw ScenarioDefinition.
internal static class ScenarioPackLoader
{
    private static readonly JsonSerializerOptions
        SerializerOptions = CreateSerializerOptions();

    internal static ScenarioDefinition Parse(
        string packJson)
    {
        ArgumentNullException.ThrowIfNull(packJson);

        ScenarioPackV1? pack;

        try
        {
            pack = JsonSerializer.Deserialize<ScenarioPackV1>(
                packJson,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "A scenario pack could not be parsed as JSON.",
                nameof(packJson),
                exception);
        }

        if (pack is null)
        {
            throw new ArgumentException(
                "A scenario pack deserialized to nothing.",
                nameof(packJson));
        }

        if (pack.FormatVersion != ScenarioPackMapper.FormatVersion)
        {
            throw new ArgumentException(
                $"Scenario pack format version {pack.FormatVersion} is not supported; expected {ScenarioPackMapper.FormatVersion}.",
                nameof(packJson));
        }

        return ScenarioPackMapper.ToRuntime(pack);
    }

    internal static ScenarioDefinition Load(
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        return Parse(File.ReadAllText(filePath));
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            IgnoreReadOnlyProperties = true
        };

        options.Converters.Add(
            new JsonStringEnumConverter(
                namingPolicy: null,
                allowIntegerValues: false));

        return options;
    }
}
