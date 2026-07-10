using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record ConditionImmunityDefinition
{
    public required ConditionType Condition { get; init; }
}