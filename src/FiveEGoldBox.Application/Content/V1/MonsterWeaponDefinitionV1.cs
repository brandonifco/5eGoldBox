namespace FiveEGoldBox.Application.Content.V1;

internal sealed record MonsterWeaponDefinitionV1
{
    public required string WeaponId { get; init; }

    public string? AmmunitionItemId { get; init; }

    public int? AmmunitionQuantity { get; init; }
}
