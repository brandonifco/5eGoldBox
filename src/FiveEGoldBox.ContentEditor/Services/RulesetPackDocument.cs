using System.Text.Json;
using System.Text.Json.Nodes;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.ContentEditor.Services;

/// A read of data/rulesets/campaign/core.json as a plain JsonObject, plus
/// the four managed content lists (and the read-only Effects reference
/// list) deserialized from it.
///
/// Deliberately re-read fresh on every operation rather than cached --
/// RulesetContentService re-reads the file at the start of every
/// load/save/delete, since Blazor Server is a long-lived process and
/// another edit could have happened between requests, and re-reading a file
/// this small is cheap.
internal sealed class RulesetPackDocument
{
    private readonly byte[] _rawBytes;
    private readonly JsonObject _root;

    private RulesetPackDocument(
        byte[] rawBytes,
        JsonObject root)
    {
        _rawBytes = rawBytes;
        _root = root;
    }

    internal static RulesetPackDocument Read(
        string filePath)
    {
        byte[] rawBytes = File.ReadAllBytes(filePath);
        string text = File.ReadAllText(filePath);

        JsonObject root = JsonNode.Parse(text)?.AsObject()
            ?? throw new InvalidOperationException(
                $"'{filePath}' did not parse to a JSON object.");

        return new RulesetPackDocument(rawBytes, root);
    }

    internal byte[] RawBytes => _rawBytes;

    internal List<WeaponDefinition> Weapons => ReadList<WeaponDefinition>("Weapons");

    internal List<SpellDefinition> Spells => ReadList<SpellDefinition>("Spells");

    internal List<EquipmentItemDefinition> EquipmentItems => ReadList<EquipmentItemDefinition>("EquipmentItems");

    internal List<MonsterDefinition> Monsters => ReadList<MonsterDefinition>("Monsters");

    internal List<EffectDefinition> Effects => ReadList<EffectDefinition>("Effects");

    private List<T> ReadList<T>(
        string propertyName)
    {
        if (_root[propertyName] is not JsonArray array)
        {
            return [];
        }

        List<T>? values = JsonSerializer.Deserialize<List<T>>(
            array.ToJsonString(),
            ContentSerialization.Options);

        return values ?? [];
    }
}
