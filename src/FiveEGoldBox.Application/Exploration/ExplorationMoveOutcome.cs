namespace FiveEGoldBox.Application.Exploration;

/// What MoveForward actually did, replacing a plain bool -- a client needs
/// to tell the player *why* a move failed (a wall versus a locked door are
/// different facts, not interchangeable "nothing happened"), and whether a
/// successful move crossed a doorway is worth a different message than an
/// ordinary step.
public enum ExplorationMoveOutcome
{
    Moved,
    MovedThroughDoorway,
    BlockedByWall,
    BlockedByLockedDoor,
}
