using System.Text.Json;
using System.Text.Json.Serialization;

namespace FiveEGoldBox.ContentEditor.Services;

/// The JsonSerializerOptions this editor reads WeaponDefinition/SpellDefinition/
/// EquipmentItemDefinition/MonsterDefinition/EffectDefinition arrays with.
///
/// Deliberately mirrors RulesetPackLoader.CreateSerializerOptions()
/// (src/FiveEGoldBox.Application/Content/RulesetPackLoader.cs) exactly --
/// same IgnoreReadOnlyProperties, same JsonStringEnumConverter(namingPolicy:
/// null, allowIntegerValues: false) -- since this editor's write path needs
/// to read back the same enum representation the loader expects.
///
/// This is only used for READING. Writing goes through
/// RulesetJsonFormatting's hand-rolled formatter instead of this options
/// object, because the committed core.json file was hand-authored with a
/// formatting style (selective single-line compaction, minimal escaping)
/// that a generic JsonSerializer pass cannot reproduce byte-for-byte -- see
/// RulesetJsonFormatting's own header comment.
internal static class RulesetSerialization
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
