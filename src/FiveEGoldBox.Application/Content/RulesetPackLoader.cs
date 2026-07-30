using System.Text.Json;
using System.Text.Json.Serialization;
using FiveEGoldBox.Application.Content.V1;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Content;

/// Reads one or more ruleset content packs from JSON and turns them into a
/// single loaded, validated ruleset.
///
/// Deliberately two entry points: Parse takes the merge-only path (string
/// content in, a raw RulesetDefinition out, no I/O and no validation) so the
/// pack-merge logic can be exercised directly in tests; Load is the real
/// entry point a registry uses, reading files from disk and running the
/// result through the same ApplicationRulesetLoader.Load pipeline every
/// other ruleset source already goes through.
internal static class RulesetPackLoader
{
    private static readonly JsonSerializerOptions
        SerializerOptions = CreateSerializerOptions();

    internal static RulesetDefinition Parse(
        IReadOnlyList<string> packJson)
    {
        ArgumentNullException.ThrowIfNull(packJson);

        RulesetPackV1[] packs = packJson
            .Select(DeserializePack)
            .ToArray();

        return RulesetPackMapper.Merge(packs);
    }

    internal static ValidatedRuleset Load(
        IReadOnlyList<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        string[] packJson = filePaths
            .Select(File.ReadAllText)
            .ToArray();

        RulesetDefinition definition = Parse(packJson);

        return ApplicationRulesetLoader.LoadOrThrow(definition);
    }

    private static RulesetPackV1 DeserializePack(
        string packJson)
    {
        RulesetPackV1? pack;

        try
        {
            pack = JsonSerializer.Deserialize<RulesetPackV1>(
                packJson,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "A ruleset pack could not be parsed as JSON.",
                nameof(packJson),
                exception);
        }

        if (pack is null)
        {
            throw new ArgumentException(
                "A ruleset pack deserialized to nothing.",
                nameof(packJson));
        }

        if (pack.FormatVersion != RulesetPackMapper.FormatVersion)
        {
            throw new ArgumentException(
                $"Ruleset pack format version {pack.FormatVersion} is not supported; expected {RulesetPackMapper.FormatVersion}.",
                nameof(packJson));
        }

        return pack;
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
