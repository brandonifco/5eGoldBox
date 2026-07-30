namespace FiveEGoldBox.Application.Content.V1;

internal sealed record ArmorDefinitionV1
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required ArmorCategoryV1 Category { get; init; }

    public required int BaseArmorClass { get; init; }

    public bool AddsDexterityModifier { get; init; }

    public int? MaximumDexterityModifier { get; init; }

    public int ArmorClassBonus { get; init; }

    public int? StrengthRequirement { get; init; }

    public bool HasStealthDisadvantage { get; init; }

    public decimal WeightPounds { get; init; }

    public int? CostInCopperPieces { get; init; }
}
