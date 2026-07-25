namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveAmmunitionV1
{
    public required string WeaponId { get; init; }

    public required string AmmunitionItemId { get; init; }

    public required int RemainingQuantity { get; init; }
}
