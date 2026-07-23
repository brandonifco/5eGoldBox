using System.Diagnostics.CodeAnalysis;
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

    internal RulesetIndex Index { get; }

    public bool TryGetBackground(
        string backgroundId,
        [NotNullWhen(true)] out BackgroundDefinition? background)
    {
        ArgumentNullException.ThrowIfNull(backgroundId);

        return Index.BackgroundsById.TryGetValue(
            backgroundId,
            out background);
    }

    public static RulesetLoadResult Load(RulesetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ValidationResult validation = RulesetValidator.Validate(definition);

        if (!validation.IsValid)
        {
            return RulesetLoadResult.Failure(validation);
        }

        RulesetIndex index = new(definition);

        return RulesetLoadResult.Success(new ValidatedRuleset(index.Ruleset, index));
    }
}
