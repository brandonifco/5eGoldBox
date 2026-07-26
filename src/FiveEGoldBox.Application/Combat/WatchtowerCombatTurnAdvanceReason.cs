namespace FiveEGoldBox.Application.Combat;

internal enum WatchtowerCombatTurnAdvanceReason
{
    PlayerEndTurn,
    StableParticipant,
    DyingParticipantAfterSave,
    NoProductiveEnemyAction,
    RaiderTurnCompleted
}
