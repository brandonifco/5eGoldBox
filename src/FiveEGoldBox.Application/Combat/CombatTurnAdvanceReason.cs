namespace FiveEGoldBox.Application.Combat;

public enum CombatTurnAdvanceReason
{
    PlayerEndTurn,
    StableParticipant,
    DyingParticipantAfterSave,
    NoProductiveEnemyAction,
    EnemyTurnCompleted
}
