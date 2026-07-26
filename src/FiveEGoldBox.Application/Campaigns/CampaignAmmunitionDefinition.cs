namespace FiveEGoldBox.Application.Campaigns;

/// Ammunition a character starts with, for the weapon that spends it.
internal sealed record CampaignAmmunitionDefinition
{
    internal required string WeaponId { get; init; }

    internal required string AmmunitionItemId { get; init; }

    internal required int Quantity { get; init; }
}
