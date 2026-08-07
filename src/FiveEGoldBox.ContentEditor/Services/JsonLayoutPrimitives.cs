using System.Globalization;
using System.Text;

namespace FiveEGoldBox.ContentEditor.Services;

/// Shape-agnostic building blocks for reproducing this repo's hand-authored
/// JSON layout (4-space indentation, selective single-line compaction,
/// minimal string escaping) byte-for-byte, extracted from
/// RulesetJsonFormatting so ScenarioJsonFormatting can reuse the same rules
/// instead of re-deriving them. See RulesetJsonFormatting's own header
/// comment for the layout rules themselves; this class only holds the
/// generic renderers, never anything shape-specific (compare
/// RulesetJsonFormatting.RenderWeapon/RenderMonster, which build a field
/// list and hand it to RenderExplodedObject here).
internal static class JsonLayoutPrimitives
{
    internal const int TopLevelArrayColumn = 4;
    internal const int TopLevelItemColumn = 8;

    internal static string RenderTopLevelArray<T>(
        IReadOnlyList<T> items,
        Func<T, int, string> renderItemExploded)
    {
        if (items.Count == 0)
        {
            return "[]";
        }

        StringBuilder sb = new();
        sb.Append('[').Append('\n');

        for (int i = 0; i < items.Count; i++)
        {
            sb.Append(Spaces(TopLevelItemColumn))
                .Append(renderItemExploded(items[i], TopLevelItemColumn));
            sb.Append(i < items.Count - 1 ? ",\n" : "\n");
        }

        sb.Append(Spaces(TopLevelArrayColumn)).Append(']');
        return sb.ToString();
    }

    internal static string? RenderNullableInt(int? value)
    {
        return value is { } v ? v.ToString(CultureInfo.InvariantCulture) : null;
    }

    internal static string RenderDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// An array whose items are always individually compact-renderable
    /// (a plain string, or a nested object made only of scalar properties):
    /// zero items omits the field entirely; one item stays on the array's
    /// own line; two or more items always explode to one item per line,
    /// matching WeaponDefinition.Properties/MonsterDefinition.AbilityModifiers
    /// in the committed file.
    internal static string? RenderCompactArray<T>(
        IReadOnlyList<T> items,
        int column,
        Func<T, string> renderCompactItem)
    {
        if (items.Count == 0)
        {
            return null;
        }

        return RenderCompactArrayCore(items, column, renderCompactItem);
    }

    /// Same layout rule as RenderCompactArray, but for a required field that
    /// must still appear as `[]` when empty rather than being omitted (e.g.
    /// MonsterDefinition.Weapons/AbilityModifiers).
    internal static string RenderCompactArrayRequired<T>(
        IReadOnlyList<T> items,
        int column,
        Func<T, string> renderCompactItem)
    {
        if (items.Count == 0)
        {
            return "[]";
        }

        return RenderCompactArrayCore(items, column, renderCompactItem);
    }

    private static string RenderCompactArrayCore<T>(
        IReadOnlyList<T> items,
        int column,
        Func<T, string> renderCompactItem)
    {
        if (items.Count == 1)
        {
            return $"[ {renderCompactItem(items[0])} ]";
        }

        StringBuilder sb = new();
        sb.Append('[').Append('\n');

        for (int i = 0; i < items.Count; i++)
        {
            sb.Append(Spaces(column)).Append(renderCompactItem(items[i]));
            sb.Append(i < items.Count - 1 ? ",\n" : "\n");
        }

        sb.Append(Spaces(column - 4)).Append(']');
        return sb.ToString();
    }

    /// An array whose items are never individually compact (they always
    /// contain a nested object/array of their own, like
    /// SpellEffectDefinition's Dice), so the array always explodes to one
    /// item per line whenever it has any items at all -- including exactly
    /// one, matching SpellDefinition.Effects in the committed file.
    internal static string? RenderAlwaysExplodedArray<T>(
        IReadOnlyList<T> items,
        int column,
        Func<T, int, string> renderItemExploded)
    {
        if (items.Count == 0)
        {
            return null;
        }

        StringBuilder sb = new();
        sb.Append('[').Append('\n');

        for (int i = 0; i < items.Count; i++)
        {
            sb.Append(Spaces(column)).Append(renderItemExploded(items[i], column));
            sb.Append(i < items.Count - 1 ? ",\n" : "\n");
        }

        sb.Append(Spaces(column - 4)).Append(']');
        return sb.ToString();
    }

    /// Same layout rule as RenderAlwaysExplodedArray, but for a required
    /// field that must still appear as `[]` when empty rather than being
    /// omitted (e.g. ExplorationFloorDefinition.Stairs -- a floor with no
    /// stairs still carries an explicit empty array in committed content).
    internal static string RenderAlwaysExplodedArrayRequired<T>(
        IReadOnlyList<T> items,
        int column,
        Func<T, int, string> renderItemExploded)
    {
        return RenderAlwaysExplodedArray(items, column, renderItemExploded) ?? "[]";
    }

    internal static string RenderCompactObject(
        IReadOnlyList<(string Key, string Value)> fields)
    {
        string body = string.Join(
            ", ",
            fields.Select(field => $"{JsonString(field.Key)}: {field.Value}"));

        return $"{{ {body} }}";
    }

    /// A top-level content record (a whole WeaponDefinition/SpellDefinition/
    /// EquipmentItemDefinition/MonsterDefinition, a SpellEffectDefinition
    /// nested inside Effects, or -- for scenario content -- a whole
    /// ScenarioLocationDefinition/TravelRouteDefinition/ShopDefinition)
    /// always renders fully exploded -- one property per line -- in the
    /// committed file, even on the rare occasion every one of its
    /// properties happens to be scalar (EquipmentItems' one entry,
    /// item.arrow, has only scalar properties and is still exploded). So
    /// this never checks for compactness; RenderCompactObject above is only
    /// used for smaller nested shapes (DamageDice, MonsterAbilityModifier,
    /// GridPosition) that are never rendered through this method.
    internal static string RenderExplodedObject(
        IReadOnlyList<(string Key, string? Value)> fields,
        int column)
    {
        List<(string Key, string Value)> present = fields
            .Where(field => field.Value is not null)
            .Select(field => (field.Key, field.Value!))
            .ToList();

        StringBuilder sb = new();
        sb.Append('{').Append('\n');

        for (int i = 0; i < present.Count; i++)
        {
            sb.Append(Spaces(column + 4))
                .Append(JsonString(present[i].Key))
                .Append(": ")
                .Append(present[i].Value);
            sb.Append(i < present.Count - 1 ? ",\n" : "\n");
        }

        sb.Append(Spaces(column)).Append('}');
        return sb.ToString();
    }

    /// Minimal JSON string escaping: only the characters that are actually
    /// illegal inside a JSON string (quote, backslash, and control
    /// characters) are escaped. Everything else -- including an apostrophe
    /// -- is written literally, matching the committed file (e.g. "Miller's
    /// thrall"), unlike System.Text.Json's default encoder, which would
    /// escape the apostrophe as '.
    internal static string JsonString(string value)
    {
        StringBuilder sb = new(value.Length + 2);
        sb.Append('"');

        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

    internal static string Spaces(int count)
    {
        return new string(' ', count);
    }

    /// A list of plain strings that stays on one line for one or two entries
    /// and only explodes to one entry per line at three or more. Returns null
    /// for an empty list so the caller's RenderExplodedObject omits the
    /// property entirely.
    ///
    /// Deliberately not the same threshold RenderCompactArray uses for
    /// ruleset content (which explodes at two or more): scenario and campaign
    /// files were authored under one convention and core.json under another.
    /// Both scenario progress-id lists and campaign skill/weapon/spell lists
    /// follow this one, verified byte-for-byte against real content -- a
    /// two-entry PreparedSpellIds stays inline, a four-entry one explodes.
    internal static string? RenderLooseStringList(
        IReadOnlyList<string> values,
        int column)
    {
        if (values.Count == 0)
        {
            return null;
        }

        if (values.Count <= 2)
        {
            return $"[ {string.Join(", ", values.Select(JsonString))} ]";
        }

        StringBuilder sb = new();
        sb.Append('[').Append('\n');

        for (int i = 0; i < values.Count; i++)
        {
            sb.Append(Spaces(column)).Append(JsonString(values[i]));
            sb.Append(i < values.Count - 1 ? ",\n" : "\n");
        }

        sb.Append(Spaces(column - 4)).Append(']');
        return sb.ToString();
    }
}
