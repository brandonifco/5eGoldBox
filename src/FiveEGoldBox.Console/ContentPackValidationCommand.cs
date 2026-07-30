using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Console;

/// The `validate` verb: checks whether a hand-authored content pack loads
/// and validates cleanly, without booting a session. Console's Main() takes
/// no other arguments, so this is the one real need that justified adding
/// argument parsing at all.
internal static class ContentPackValidationCommand
{
    private const string Usage =
        "Usage: validate <ruleset|scenario|campaign> <path> [<path>...]";

    internal static int Run(
        IReadOnlyList<string> arguments,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (arguments.Count < 2)
        {
            error.WriteLine(Usage);
            return 2;
        }

        string kind = arguments[0];
        string[] paths = arguments.Skip(1).ToArray();
        ValidationResult result;

        switch (kind)
        {
            case "ruleset":
                result = ContentPackValidation.ValidateRulesetPack(paths);
                break;

            case "scenario":
                if (!TryRequireSinglePath(paths, "scenario", error, out string scenarioPath))
                {
                    return 2;
                }

                result = ContentPackValidation.ValidateScenarioPack(scenarioPath);
                break;

            case "campaign":
                if (!TryRequireSinglePath(paths, "campaign", error, out string campaignPath))
                {
                    return 2;
                }

                result = ContentPackValidation.ValidateCampaignPack(campaignPath);
                break;

            default:
                error.WriteLine($"Unknown content kind '{kind}'. {Usage}");
                return 2;
        }

        return Report(result, output);
    }

    private static bool TryRequireSinglePath(
        IReadOnlyList<string> paths,
        string kind,
        TextWriter error,
        out string path)
    {
        if (paths.Count != 1)
        {
            error.WriteLine($"A {kind} pack is a single file; only one path is accepted.");
            path = "";
            return false;
        }

        path = paths[0];
        return true;
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
