using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed record CharacterConditionImmunity
{
    public required ConditionType Condition { get; init; }
}