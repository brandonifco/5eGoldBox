namespace FiveEGoldBox.Application.Content.V1;

internal sealed record CampaignAmmunitionDefinitionV1
{
    public required string WeaponId { get; init; }

    public required string AmmunitionItemId { get; init; }

    public required int Quantity { get; init; }
}
