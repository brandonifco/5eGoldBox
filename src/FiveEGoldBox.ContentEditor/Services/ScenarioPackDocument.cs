using System.Text;
using System.Text.Json;
using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Services;

/// A read of one data/scenarios/&lt;id&gt;/scenario.json as a plain
/// ScenarioPackV1, plus the file's own raw bytes for RulesetJsonSplicer to
/// operate on.
///
/// Unlike RulesetPackDocument (which reads per-property JsonArrays, since a
/// ruleset pack can in principle be merged from several files), a scenario
/// pack is always exactly one file, so this deserializes the whole document
/// in one call -- the same thing ScenarioPackLoader.Parse does internally,
/// just producing the DTO directly instead of the mapped runtime
/// ScenarioDefinition, since the editor needs the DTO's own optional/default
/// fields (e.g. Shops defaulting to empty) rather than the runtime shape.
///
/// Deliberately re-read fresh on every operation, same reasoning as
/// RulesetPackDocument's own header comment.
internal sealed class ScenarioPackDocument
{
    private readonly byte[] _rawBytes;
    private readonly ScenarioPackV1 _pack;

    private ScenarioPackDocument(
        byte[] rawBytes,
        ScenarioPackV1 pack)
    {
        _rawBytes = rawBytes;
        _pack = pack;
    }

    internal static ScenarioPackDocument Read(
        string filePath)
    {
        byte[] rawBytes = File.ReadAllBytes(filePath);

        ScenarioPackV1 pack = JsonSerializer.Deserialize<ScenarioPackV1>(
            rawBytes,
            ContentSerialization.Options)
            ?? throw new InvalidOperationException(
                $"'{filePath}' did not parse to a scenario pack.");

        return new ScenarioPackDocument(rawBytes, pack);
    }

    internal byte[] RawBytes => _rawBytes;

    internal string ScenarioId => _pack.ScenarioId;

    internal string DisplayName => _pack.DisplayName;

    internal string RulesetId => _pack.RulesetId;

    internal string StartingLocationId => _pack.StartingLocationId;

    internal IReadOnlyList<ScenarioLocationDefinitionV1> Locations => _pack.Locations;

    internal IReadOnlyList<TravelRouteDefinitionV1> Routes => _pack.Routes;

    internal IReadOnlyList<ShopDefinitionV1> Shops => _pack.Shops;

    internal IReadOnlyList<ScenarioTriggerDefinitionV1> Triggers => _pack.Triggers;

    internal IReadOnlyList<ScenarioDecisionDefinitionV1> Decisions => _pack.Decisions;

    /// Exposed read-only for a trigger's encounter picker. Encounters are not
    /// editable here yet -- that's a later phase -- but they are readable, so
    /// a trigger can point at a real one today instead of a typed-in id.
    internal IReadOnlyList<EncounterDefinitionV1> Encounters => _pack.Encounters;

    /// The scenario's own closed set of progress-id strings -- the picker
    /// source for ExplorableProgressIds/RequiredProgressIds checkbox groups,
    /// the same way ruleset content's cross-references get pickers rather
    /// than free text.
    internal IReadOnlyList<string> ProgressIds => _pack.Progress.ProgressIds;

    /// The exact original bytes of one location's "ExplorationMap" value,
    /// verbatim -- null if that location has none. ExplorationMap isn't
    /// editable through this editor yet (see ScenarioLocationFormModel), so
    /// whenever any location in this file is saved, every OTHER location's
    /// map must round-trip completely unchanged. Re-deriving it from
    /// ExplorationMapDefinitionV1 through a hand-rolled formatter (the way
    /// ScenarioJsonFormatting renders everything else) turned out not to
    /// work for this one field: real committed content orders a floor's
    /// Doors/Treasures/Npcs differently from file to file (confirmed --
    /// Hollow Mill writes Npcs before Doors/Treasures, Watchtower writes
    /// Doors/Treasures before Npcs), so no single fixed field order
    /// reproduces both byte-for-byte. Copying the original bytes verbatim
    /// sidesteps needing to reverse-engineer (or care about) that ordering
    /// at all.
    internal string? FindRawExplorationMapText(
        string locationId)
    {
        Utf8JsonReader reader = new(_rawBytes);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("The scenario pack's root is not a JSON object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1)
            {
                continue;
            }

            if (!reader.ValueTextEquals("Locations"))
            {
                reader.Read();
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
                continue;
            }

            reader.Read(); // StartArray

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    continue;
                }

                (bool MatchesId, (int Start, int End)? MapSpan) location = ScanLocationObject(ref reader, locationId);

                if (location.MatchesId)
                {
                    return location.MapSpan is { } span
                        ? Encoding.UTF8.GetString(_rawBytes, span.Start, span.End - span.Start)
                        : null;
                }
            }

            return null;
        }

        return null;
    }

    /// Reads through one location object's properties (the reader must be
    /// positioned on its StartObject token), capturing whether its
    /// LocationId matches and, if present, its ExplorationMap value's byte
    /// span -- leaving the reader positioned on this object's EndObject.
    private (bool MatchesId, (int Start, int End)? MapSpan) ScanLocationObject(
        ref Utf8JsonReader reader,
        string locationId)
    {
        int propertyDepth = reader.CurrentDepth + 1;
        bool matchesId = false;
        (int Start, int End)? mapSpan = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != propertyDepth)
            {
                continue;
            }

            if (reader.ValueTextEquals("LocationId"))
            {
                reader.Read();
                matchesId = reader.ValueTextEquals(locationId);
            }
            else if (reader.ValueTextEquals("ExplorationMap"))
            {
                reader.Read();

                int start = checked((int)reader.TokenStartIndex);

                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }

                int end = checked((int)reader.BytesConsumed);
                mapSpan = (start, end);
            }
            else
            {
                reader.Read();
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
            }
        }

        return (matchesId, mapSpan);
    }
}
