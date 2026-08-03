namespace FiveEGoldBox.Application.Views;

/// What a client can offer the party right now.
///
/// One value per verb the engine exposes, so a client can bind a control to
/// each without knowing which scenario is running or what its places are
/// called.
public enum SessionActionKind
{
    ResolveOutpostDecision,
    BeginJourney,
    AdvanceTravel,
    EnterDestination,
    MoveForward,
    TurnLeft,
    TurnRight,
    UseStairs,
    ActivateTrigger,
    OpenDoor,
    RevealSecretDoor,
    CollectTreasure,
    TalkToNpc,
    PurchaseShopItem
}
