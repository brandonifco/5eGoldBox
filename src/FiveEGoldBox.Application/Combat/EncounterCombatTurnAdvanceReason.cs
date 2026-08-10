namespace FiveEGoldBox.Application.Combat;

internal enum EncounterCombatTurnAdvanceReason
{
    PlayerEndTurn,
    StableParticipant,
    DyingParticipantAfterSave,
    NoProductiveEnemyAction,
    RaiderTurnCompleted
}
