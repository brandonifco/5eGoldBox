using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Content;

/// The read-only entry point a tool uses to check whether a hand-authored
/// content pack loads and validates cleanly, without booting a session.
///
/// Console's game boot and the *Registry classes call the same loaders and
/// validators these methods wrap, but they run inside pipelines built for
/// play, which throw on the first problem found. A caller that only wants
/// to know "is this file good" wants every issue collected and returned
/// instead, so each method here catches the exceptions those pipelines
/// still throw for a malformed file (bad JSON, an unsupported format
/// version) and folds them into the same ValidationResult shape as a
/// structural content issue.
public static class ContentPackValidation
{
    public static ValidationResult ValidateRulesetPack(
        IReadOnlyList<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        RulesetDefinition definition;

        try
        {
            string[] packJson = filePaths
                .Select(File.ReadAllText)
                .ToArray();

            definition = RulesetPackLoader.Parse(packJson);
        }
        catch (Exception exception) when (IsContentLoadFailure(exception))
        {
            return ParseFailure(exception);
        }

        return ApplicationRulesetLoader.Load(definition).Validation;
    }

    public static ValidationResult ValidateScenarioPack(
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        ScenarioDefinition definition;

        try
        {
            definition = ScenarioPackLoader.Load(filePath);
        }
        catch (Exception exception) when (IsContentLoadFailure(exception))
        {
            return ParseFailure(exception);
        }

        return ScenarioDefinitionValidator.Validate(definition);
    }

    public static ValidationResult ValidateCampaignPack(
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        CampaignDefinition definition;

        try
        {
            definition = CampaignPackLoader.Load(filePath);
        }
        catch (Exception exception) when (IsContentLoadFailure(exception))
        {
            return ParseFailure(exception);
        }

        try
        {
            CampaignDefinitionValidator.Validate(definition);
        }
        catch (ArgumentException exception)
        {
            return new ValidationResult([
                new ValidationIssue(
                    ValidationSeverity.Error,
                    "content.campaign.invalid",
                    exception.Message)
            ]);
        }

        return ValidationResult.Success;
    }

    private static bool IsContentLoadFailure(
        Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => false,
            ArgumentException => true,
            IOException => true,
            _ => false
        };
    }

    private static ValidationResult ParseFailure(
        Exception exception)
    {
        return new ValidationResult([
            new ValidationIssue(
                ValidationSeverity.Error,
                "content.pack.parse_failed",
                exception.Message)
        ]);
    }
}
