using System.Text.Json;
using System.Text.Json.Nodes;
using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Console;

/// The `import-map` verb: converts a Tiled map editor JSON export into one
/// location's ExplorationMap (plus any tile-anchored Triggers) and merges
/// it into an existing scenario file, then reports whether the result
/// validates -- the same "report, don't gatekeep" shape `validate` uses.
internal static class TiledMapImportCommand
{
    private const string Usage =
        "Usage: import-map <tiled-file> <scenario-file> <location-id> <map-id> [<display-name>]";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    internal static int Run(
        IReadOnlyList<string> arguments,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (arguments.Count is < 4 or > 5)
        {
            error.WriteLine(Usage);
            return 2;
        }

        string tiledPath = arguments[0];
        string scenarioPath = arguments[1];
        string locationId = arguments[2];
        string mapId = arguments[3];
        string? displayName = arguments.Count == 5 ? arguments[4] : null;

        if (!TryReadFile(tiledPath, "content.tiled_import.tiled_file_not_found", "Tiled file", out string tiledJson, out ValidationIssue? readIssue))
        {
            return Report(new ValidationResult([readIssue!]), output);
        }

        TiledConversionResult conversion = TiledMapConverter.Convert(tiledJson, mapId, locationId);

        if (!conversion.Succeeded)
        {
            return Report(new ValidationResult(conversion.Issues), output);
        }

        if (!TryReadFile(scenarioPath, "content.tiled_import.scenario_file_not_found", "scenario file", out string scenarioJson, out ValidationIssue? scenarioReadIssue))
        {
            return Report(new ValidationResult([scenarioReadIssue!]), output);
        }

        JsonObject scenarioRoot;

        try
        {
            scenarioRoot = JsonNode.Parse(scenarioJson)?.AsObject()
                ?? throw new JsonException("The scenario document is empty.");
        }
        catch (JsonException exception)
        {
            return Report(new ValidationResult([
                new ValidationIssue(
                    ValidationSeverity.Error,
                    "content.tiled_import.scenario_file_malformed",
                    $"Could not parse '{scenarioPath}': {exception.Message}")
            ]), output);
        }

        IReadOnlyList<ValidationIssue> mergeIssues = TiledMapConverter.Merge(
            scenarioRoot,
            conversion.ExplorationMap!,
            conversion.Triggers,
            locationId,
            displayName);

        if (mergeIssues.Count > 0)
        {
            return Report(new ValidationResult(mergeIssues), output);
        }

        File.WriteAllText(scenarioPath, scenarioRoot.ToJsonString(WriteOptions));

        return Report(ContentPackValidation.ValidateScenarioPack(scenarioPath), output);
    }

    private static bool TryReadFile(
        string path,
        string issueCode,
        string description,
        out string contents,
        out ValidationIssue? issue)
    {
        try
        {
            contents = File.ReadAllText(path);
            issue = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            contents = "";
            issue = new ValidationIssue(
                ValidationSeverity.Error,
                issueCode,
                $"Could not read {description} '{path}': {exception.Message}");
            return false;
        }
    }

    private static int Report(
        ValidationResult result,
        TextWriter output)
    {
        if (result.Issues.Count == 0)
        {
            output.WriteLine("Valid: no issues found.");
            return 0;
        }

        foreach (ValidationIssue issue in result.Issues)
        {
            output.WriteLine($"{issue.Severity} {issue.Code}: {issue.Message}");
        }

        return result.IsValid ? 0 : 1;
    }
}
