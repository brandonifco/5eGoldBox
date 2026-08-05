using System.Text.Json;
using System.Text.Json.Serialization;

namespace FiveEGoldBox.ContentEditor.Services;

/// The JsonSerializerOptions this editor reads content arrays with --
/// ruleset content (WeaponDefinition/SpellDefinition/EquipmentItemDefinition/
/// MonsterDefinition/EffectDefinition, the public Core.Definitions types)
/// and scenario content (ScenarioLocationDefinitionV1/TravelRouteDefinitionV1/
/// ShopDefinitionV1 and their nested V1 DTOs, internal to
/// FiveEGoldBox.Application and reachable here only because of the
/// InternalsVisibleTo grant to this project).
///
/// Deliberately mirrors RulesetPackLoader.CreateSerializerOptions() and
/// ScenarioPackLoader's own (private) equivalent
/// (src/FiveEGoldBox.Application/Content/{RulesetPackLoader,ScenarioPackLoader}.cs)
/// exactly -- same IgnoreReadOnlyProperties, same
/// JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) --
/// since this editor's write path needs to read back the same enum
/// representation those loaders expect.
///
/// This is only used for READING. Writing goes through
/// RulesetJsonFormatting/ScenarioJsonFormatting's hand-rolled formatters
/// instead of this options object, because the committed JSON files were
/// hand-authored with formatting styles (selective single-line compaction,
/// minimal escaping) a generic JsonSerializer pass cannot reproduce
/// byte-for-byte -- see those classes' own header comments.
internal static class ContentSerialization
{
    internal static readonly JsonSerializerOptions Options = CreateSerializerOptions();

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
