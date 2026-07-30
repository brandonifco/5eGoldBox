namespace FiveEGoldBox.Application.Content.V1;

internal sealed record CombatantWeaponDefinitionV1
{
    public required string WeaponId { get; init; }

    public string? AmmunitionItemId { get; init; }

    public int? AmmunitionQuantity { get; init; }
}
