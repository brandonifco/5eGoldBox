using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Outposts;

/// Spending the party's shared purse at a hub-level shop -- always
/// available whenever the party occupies its location, unlike
/// <see cref="OutpostDecisionRules"/>'s one-time, progress-gated choices.
public static class ShopRules
{
    public static bool CanPurchase(
        ApplicationSessionState session,
        string itemId)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            return false;
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);
        ShopItemDefinition? item = FindShopItem(canonicalSession, itemId);

        return item is not null
            && canonicalSession.Party.Currency.GoldPieces
                >= item.PriceGoldPieces;
    }

    public static ApplicationSessionState Purchase(
        ApplicationSessionState session,
        string itemId)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException(
                "An item ID is required.",
                nameof(itemId));
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.CurrentMode != ApplicationMode.Outpost)
        {
            throw new InvalidOperationException(
                "Shops are available only at a hub.");
        }

        ShopItemDefinition item =
            FindShopItem(canonicalSession, itemId)
            ?? throw new InvalidOperationException(
                $"'{itemId}' is not sold here.");

        PartyState party = canonicalSession.Party;

        if (party.Currency.GoldPieces < item.PriceGoldPieces)
        {
            throw new InvalidOperationException(
                "The party cannot afford this.");
        }

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                Party = party with
                {
                    Currency = party.Currency with
                    {
                        GoldPieces = party.Currency.GoldPieces
                            - item.PriceGoldPieces
                    },
                    InventoryItems = PartyInventoryRules.AddItem(
                        party.InventoryItems,
                        item.ItemId,
                        1)
                }
            });
    }

    /// The shop at the party's current hub, if the scenario declares one
    /// there. Internal rather than private so SessionView can list its
    /// goods without ShopRules exposing ShopDefinition/ShopItemDefinition
    /// themselves.
    internal static ShopDefinition? FindShopHere(
        ApplicationSessionState session)
    {
        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            return null;
        }

        return ScenarioDefinitionRegistry
            .Resolve(session)
            .Shops
            .FirstOrDefault(shop => string.Equals(
                shop.LocationId,
                session.CurrentLocationId,
                StringComparison.Ordinal));
    }

    private static ShopItemDefinition? FindShopItem(
        ApplicationSessionState session,
        string itemId)
    {
        return FindShopHere(session)
            ?.Items
            .FirstOrDefault(item => string.Equals(
                item.ItemId,
                itemId,
                StringComparison.Ordinal));
    }
}
