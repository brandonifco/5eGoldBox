namespace FiveEGoldBox.Application.Parties;

public sealed record AmmunitionState
{
    public required string WeaponId { get; init; }

    public required string AmmunitionItemId { get; init; }

    public required int RemainingQuantity { get; init; }
}
