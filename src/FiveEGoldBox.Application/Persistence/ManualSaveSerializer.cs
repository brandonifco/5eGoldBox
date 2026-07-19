using System.Text.Json;
using System.Text.Json.Serialization;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Persistence;

public static class ManualSaveSerializer
{
    public const int SupportedFormatVersion = 1;

    private static readonly JsonSerializerOptions
        SerializerOptions = CreateSerializerOptions();

    public static string Serialize(
        ApplicationSessionState session)
    {
        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        ValidateSaveableMode(canonicalSession);

        ManualSaveData saveData = new()
        {
            FormatVersion = SupportedFormatVersion,
            Session = canonicalSession
        };

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

        ManualSaveData? saveData;

        try
        {
            saveData = JsonSerializer.Deserialize<ManualSaveData>(
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

        if (saveData.FormatVersion
            != SupportedFormatVersion)
        {
            return ManualSaveLoadResult.Failure(
                ManualSaveLoadFailureReason
                    .UnsupportedFormatVersion);
        }

        try
        {
            ApplicationSessionState canonicalSession =
                ApplicationSessionRules.CreateCanonical(
                    saveData.Session);

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

    private static void ValidateSaveableMode(
        ApplicationSessionState session)
    {
        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            throw new ArgumentException(
                "Only outpost sessions can be stored in the manual save during this application phase.",
                nameof(session));
        }
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
