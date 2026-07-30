using System.Text.Json;
using System.Text.Json.Serialization;
using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.Application.Content;

/// Reads one campaign content pack from JSON.
///
/// Like ScenarioPackLoader, there is no merge step and no validate-or-throw
/// wrapper here -- CampaignRegistry already validates whatever its factory
/// returns (via CampaignDefinitionValidator) before caching it.
internal static class CampaignPackLoader
{
    private static readonly JsonSerializerOptions
        SerializerOptions = CreateSerializerOptions();

    internal static CampaignDefinition Parse(
        string packJson)
    {
        ArgumentNullException.ThrowIfNull(packJson);

        CampaignPackV1? pack;

        try
        {
            pack = JsonSerializer.Deserialize<CampaignPackV1>(
                packJson,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "A campaign pack could not be parsed as JSON.",
                nameof(packJson),
                exception);
        }

        if (pack is null)
        {
            throw new ArgumentException(
                "A campaign pack deserialized to nothing.",
                nameof(packJson));
        }

        if (pack.FormatVersion != CampaignPackMapper.FormatVersion)
        {
            throw new ArgumentException(
                $"Campaign pack format version {pack.FormatVersion} is not supported; expected {CampaignPackMapper.FormatVersion}.",
                nameof(packJson));
        }

        return CampaignPackMapper.ToRuntime(pack);
    }

    internal static CampaignDefinition Load(
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
