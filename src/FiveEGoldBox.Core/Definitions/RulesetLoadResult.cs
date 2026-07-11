using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Definitions;

public sealed record RulesetLoadResult(
    ValidationResult Validation,
    ValidatedRuleset? Ruleset)
{
    public bool IsValid => Validation.IsValid && Ruleset is not null;

    public static RulesetLoadResult Success(ValidatedRuleset ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        return new RulesetLoadResult(ValidationResult.Success, ruleset);
    }

    public static RulesetLoadResult Failure(ValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        return new RulesetLoadResult(validation, null);
    }
}
