using System.Text;
using System.Text.Json;

namespace FiveEGoldBox.ContentEditor.Services;

/// Replaces exactly one root-level property's value in a JSON document's raw
/// bytes, leaving every other byte untouched.
///
/// This is the mechanism that makes a byte-identical no-op save possible.
/// Re-parsing the whole document into a JsonNode/JsonObject tree and calling
/// ToJsonString() on it again -- the more obvious approach -- necessarily
/// re-serializes every byte of the file, including the parts nobody
/// touched, and data/rulesets/campaign/core.json was hand-authored with a
/// formatting style (see RulesetJsonFormatting's header comment) that no
/// generic serializer reproduces. Splicing only the one array actually
/// being written sidesteps the problem instead of trying to solve it in
/// general: everything outside that array's byte span is copied forward
/// unchanged by construction.
internal static class RulesetJsonSplicer
{
    /// Finds the byte offsets of a root-level property's value in a JSON
    /// document (the object's own top-level members only, not any
    /// same-named property nested deeper -- MonsterDefinition.Weapons, for
    /// instance, must never be confused with the root's own "Weapons").
    internal static (int Start, int End) FindRootPropertyValueSpan(
        byte[] documentBytes,
        string propertyName)
    {
        ArgumentNullException.ThrowIfNull(documentBytes);
        ArgumentNullException.ThrowIfNull(propertyName);

        Utf8JsonReader reader = new(documentBytes);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException(
                "The ruleset pack's root is not a JSON object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName
                && reader.CurrentDepth == 1)
            {
                bool isMatch = reader.ValueTextEquals(propertyName);

                if (isMatch)
                {
                    reader.Read();

                    long start = reader.TokenStartIndex;

                    if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                    {
                        reader.Skip();
                    }

                    long end = reader.BytesConsumed;

                    return (checked((int)start), checked((int)end));
                }

                // Not the property we want -- skip its value without
                // descending into it, so a same-named property nested
                // inside it (e.g. a monster's own "Weapons" list) can never
                // be mistaken for the root's.
                reader.Read();
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
            }
        }

        throw new InvalidOperationException(
            $"The ruleset pack has no root-level property named '{propertyName}'.");
    }

    /// Replaces the named root-level property's value with replacementText,
    /// returning the whole document's new bytes. Every byte outside that
    /// property's value span is copied forward unchanged.
    internal static byte[] ReplaceRootPropertyValue(
        byte[] documentBytes,
        string propertyName,
        string replacementText)
    {
        ArgumentNullException.ThrowIfNull(documentBytes);
        ArgumentNullException.ThrowIfNull(replacementText);

        (int start, int end) = FindRootPropertyValueSpan(documentBytes, propertyName);

        byte[] replacementBytes = Encoding.UTF8.GetBytes(replacementText);

        byte[] result = new byte[documentBytes.Length - (end - start) + replacementBytes.Length];

        Buffer.BlockCopy(documentBytes, 0, result, 0, start);
        Buffer.BlockCopy(replacementBytes, 0, result, start, replacementBytes.Length);
        Buffer.BlockCopy(
            documentBytes,
            end,
            result,
            start + replacementBytes.Length,
            documentBytes.Length - end);

        return result;
    }
}
