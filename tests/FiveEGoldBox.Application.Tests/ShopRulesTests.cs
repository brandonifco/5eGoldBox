using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Tests;

/// Covers the general store at Hollow Mill's village hub -- the scenario's
/// only authored shop today.
public sealed class ShopRulesTests
{
    private const int RandomSeed = 99;

    [Fact]
    public void CanPurchase_WithEnoughGold_ReturnsTrue()
    {
        Assert.True(ShopRules.CanPurchase(WithGold(5), "item.torch"));
    }

    [Fact]
    public void CanPurchase_WithoutEnoughGold_ReturnsFalse()
    {
        Assert.False(ShopRules.CanPurchase(WithGold(0), "item.torch"));
    }

    [Fact]
    public void CanPurchase_UnknownItem_ReturnsFalse()
    {
        Assert.False(
            ShopRules.CanPurchase(WithGold(100), "item.does-not-exist"));
    }

    [Fact]
    public void CanPurchase_OutsideOutpostMode_ReturnsFalse()
    {
        Assert.False(ShopRules.CanPurchase(TravelingSession(), "item.torch"));
    }

    [Fact]
    public void CanPurchase_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ShopRules.CanPurchase(null!, "item.torch"));
    }

    [Fact]
    public void Purchase_DeductsGoldAndGrantsTheItem()
    {
        ApplicationSessionState purchased =
            ShopRules.Purchase(WithGold(5), "item.torch");

        Assert.Equal(4, purchased.Party.Currency.GoldPieces);

        PartyInventoryItemState entry = Assert.Single(
            purchased.Party.InventoryItems,
            item => item.ItemId == "item.torch");
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void Purchase_BuyingTheSameItemTwiceStacksIt()
    {
        ApplicationSessionState oncePurchased =
            ShopRules.Purchase(WithGold(10), "item.torch");
        ApplicationSessionState twicePurchased =
            ShopRules.Purchase(oncePurchased, "item.torch");

        Assert.Equal(8, twicePurchased.Party.Currency.GoldPieces);

        PartyInventoryItemState entry = Assert.Single(
            twicePurchased.Party.InventoryItems,
            item => item.ItemId == "item.torch");
        Assert.Equal(2, entry.Quantity);
    }

    [Fact]
    public void Purchase_DoesNotDisturbOtherCurrencyOrItems()
    {
        ApplicationSessionState session = WithGold(5) with
        {
            Party = WithGold(5).Party with
            {
                Currency = WithGold(5).Party.Currency with
                {
                    SilverPieces = 7
                }
            }
        };

        ApplicationSessionState purchased =
            ShopRules.Purchase(session, "item.torch");

        Assert.Equal(7, purchased.Party.Currency.SilverPieces);
    }

    [Fact]
    public void Purchase_InsufficientFunds_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ShopRules.Purchase(WithGold(0), "item.torch"));
    }

    [Fact]
    public void Purchase_UnknownItem_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ShopRules.Purchase(WithGold(100), "item.does-not-exist"));
    }

    [Fact]
    public void Purchase_OutsideOutpostMode_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ShopRules.Purchase(TravelingSession(), "item.torch"));
    }

    [Fact]
    public void Purchase_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ShopRules.Purchase(null!, "item.torch"));
    }

    private static ApplicationSessionState WithGold(int goldPieces)
    {
        ApplicationSessionState session = ScenarioSessionFactory.CreateNew(
            HollowMillScenarioIds.ScenarioId,
            RandomSeed);

        return session with
        {
            Party = session.Party with
            {
                Currency = session.Party.Currency with
                {
                    GoldPieces = goldPieces
                }
            }
        };
    }

    private static ApplicationSessionState TravelingSession()
    {
        ApplicationSessionState session = ScenarioSessionFactory.CreateNew(
            HollowMillScenarioIds.ScenarioId,
            RandomSeed);

        session = OutpostDecisionRules.Resolve(session, "AcceptMission").State;

        return RegionalTravelRules.BeginJourney(
            session,
            HollowMillScenarioIds.RouteId);
    }
}
