using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Definitions;

public sealed class ValidatedRuleset
{
    private ValidatedRuleset(RulesetDefinition definition, RulesetIndex index)
    {
        Definition = definition;
        Index = index;
    }

    public RulesetDefinition Definition { get; }

    public RulesetIndex Index { get; }

    public static RulesetLoadResult Load(RulesetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ValidationResult validation = RulesetValidator.Validate(definition);

        if (!validation.IsValid)
        {
            return RulesetLoadResult.Failure(validation);
        }

        RulesetIndex index = new(definition);

        return RulesetLoadResult.Success(new ValidatedRuleset(definition, index));
    }
}
