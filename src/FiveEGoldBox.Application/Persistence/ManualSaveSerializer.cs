using System.Text.Json;
using System.Text.Json.Serialization;
using FiveEGoldBox.Application.Persistence.V1;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Persistence;

public static class ManualSaveSerializer
{
    public const int SupportedFormatVersion = SaveGameMapper.FormatVersion;

    private static readonly JsonSerializerOptions
        SerializerOptions = CreateSerializerOptions();

    public static bool CanSerialize(
        ApplicationSessionState session)
    {
        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        return IsSaveableMode(
            canonicalSession.CurrentMode);
    }

    public static string Serialize(
        ApplicationSessionState session)
    {
        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        ValidateSaveableMode(canonicalSession);

        SaveGameV1 saveData = SaveGameMapper.ToSaveV1(
            canonicalSession);

        return JsonSerializer.Serialize(
            saveData,
            SerializerOptions);
    }

    public static ManualSaveLoadResult Deserialize(
        string? serializedData)
    {
        if (string.IsNullOrWhiteSpace(serializedData))
        {
            return ManualSaveLoadResult.Failure(
                ManualSaveLoadFailureReason
                    .MalformedSerializedData);
        }

        if (!TryReadFormatVersion(
            serializedData,
            out int formatVersion))
        {
            return ManualSaveLoadResult.Failure(
                ManualSaveLoadFailureReason
                    .MalformedSerializedData);
        }

        // Every future format version is added as another arm here that
        // deserializes into its own version-specific DTO and maps forward
        // to the current runtime state; JSON is never deserialized into a
        // versioned DTO before its declared FormatVersion is known.
        return formatVersion switch
        {
            SaveGameMapper.FormatVersion => DeserializeV1(serializedData),
            _ => ManualSaveLoadResult.Failure(
                ManualSaveLoadFailureReason
                    .UnsupportedFormatVersion)
        };
    }

    private static ManualSaveLoadResult DeserializeV1(
        string serializedData)
    {
        SaveGameV1? saveData;

        try
        {
            saveData = JsonSerializer.Deserialize<SaveGameV1>(
                serializedData,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return ManualSaveLoadResult.Failure(
                ManualSaveLoadFailureReason
                    .MalformedSerializedData);
        }
        catch (NotSupportedException)
        {
            return ManualSaveLoadResult.Failure(
                ManualSaveLoadFailureReason
                    .MalformedSerializedData);
        }

        if (saveData is null)
        {
            return ManualSaveLoadResult.Failure(
                ManualSaveLoadFailureReason
                    .MalformedSerializedData);
        }

        try
        {
            ApplicationSessionState runtimeSession =
                SaveGameMapper.ToRuntime(saveData);

            ApplicationSessionState canonicalSession =
                ApplicationSessionRules.CreateCanonical(
                    runtimeSession);

            ValidateSaveableMode(canonicalSession);

            return ManualSaveLoadResult.Success(
                canonicalSession);
        }
        catch (ArgumentException)
        {
            return ManualSaveLoadResult.Failure(
                ManualSaveLoadFailureReason
                    .InvalidSessionState);
        }
    }

    private static bool TryReadFormatVersion(
        string serializedData,
        out int formatVersion)
    {
        formatVersion = default;

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                serializedData);

            if (document.RootElement.ValueKind
                    != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(
                    "FormatVersion",
                    out JsonElement versionElement)
                || versionElement.ValueKind
                    != JsonValueKind.Number
                || !versionElement.TryGetInt32(
                    out formatVersion))
            {
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void ValidateSaveableMode(
        ApplicationSessionState session)
    {
        if (!IsSaveableMode(session.CurrentMode))
        {
            throw new ArgumentException(
                "Only outpost, regional-travel, exploration, and scenario-conclusion sessions can be stored in the manual save during this application phase.",
                nameof(session));
        }
    }

    // RegionalTravelState was always simple, already-resumable data — the V1
    // DTO and SaveGameMapper have carried it since the format's first
    // version, in case a mode needed to save mid-journey once one did. The
    // only reason it couldn't was this gate. Encounter is not here: a
    // combat's turn order and mid-resolution rolls are not comparably
    // simple, and V1's ActiveEncounter is still the deliberate empty
    // placeholder it always was.
    private static bool IsSaveableMode(
        ApplicationMode mode)
    {
        return mode is
            ApplicationMode.Outpost
            or ApplicationMode.RegionalTravel
            or ApplicationMode.Exploration
            or ApplicationMode.ScenarioConclusion;
    }

    private static JsonSerializerOptions
        CreateSerializerOptions()
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
