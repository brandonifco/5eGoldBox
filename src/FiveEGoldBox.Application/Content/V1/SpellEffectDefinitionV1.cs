namespace FiveEGoldBox.Application.Content.V1;

internal sealed record SpellEffectDefinitionV1
{
    public required SpellEffectKindV1 Kind { get; init; }

    public required DamageDiceV1 Dice { get; init; }

    public int Instances { get; init; } = 1;

    public bool AddsSpellcastingModifier { get; init; }

    public string? DamageType { get; init; }
}
