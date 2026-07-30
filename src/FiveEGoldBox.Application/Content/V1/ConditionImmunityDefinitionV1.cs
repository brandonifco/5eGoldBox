namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ConditionImmunityDefinitionV1
{
    public required ConditionTypeV1 Condition { get; init; }
}
