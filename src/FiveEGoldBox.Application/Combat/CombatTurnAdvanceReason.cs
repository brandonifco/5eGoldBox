namespace FiveEGoldBox.Application.Combat;

public enum CombatTurnAdvanceReason
{
    PlayerEndTurn,
    StableParticipant,
    DyingParticipantAfterSave,

    /// Knocked out during their own turn — only a reaction can do this,
    /// so before opportunity attacks existed it could not happen at all.
    DroppedOnOwnTurn,
    NoProductiveEnemyAction,
    EnemyTurnCompleted
}
